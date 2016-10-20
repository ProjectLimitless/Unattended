/** 
* This file is part of Unattended.
* Copyright © 2016 Donovan Solms.
* Project Limitless
* https://www.projectlimitless.io
* 
* Unattended and Project Limitless is free software: you can redistribute it and/or modify
* it under the terms of the Apache License Version 2.0.
* 
* You should have received a copy of the Apache License Version 2.0 with
* Unattended. If not, see http://www.apache.org/licenses/LICENSE-2.0.
*/
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Security.Cryptography;

using NLog;
using Limitless.ioRPC;
using Limitless.ioRPC.Structs;
using Limitless.Unattended.Enums;
using Limitless.Unattended.Structs;
using Limitless.Unattended.Extensions;
using Limitless.Unattended.Configuration;

namespace Limitless.Unattended
{
    /// <summary>
    /// Unattended core, runs the update lifecycle.
    /// </summary>
    public class Unattended
    {
        /// <summary>
        /// Lock for syncing.
        /// </summary>
        private static readonly object syncLock = new object();

        /// <summary>
        /// NLog logger.
        /// </summary>
        private Logger log;
        /// <summary>
        /// The installed client ID.
        /// </summary>
        private string clientId;
        /// <summary>
        /// The configured and validated update strategy.
        /// </summary>
        private string updateStrategy;
        /// <summary>
        /// The configured and validated update interval.
        /// </summary>
        private string updateInterval;
        /// <summary>
        /// Collection of the updates to check at the interval.
        /// </summary>
        private List<UpdateManifest> updateManifests;
        /// <summary>
        /// Flag to check if we are currently in update state.
        /// </summary>
        private volatile bool isUpdating;
        /// <summary>
        /// ioRPC server instance.
        /// </summary>
        private Server ioServer;
        /// <summary>
        /// The target application.
        /// </summary>
        private Target target;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Unattended()
        {
            // Create defaults
            UnattendedSection settings = new UnattendedSection();
            setup(settings);
        }

        /// <summary>
        /// Standard constructor with predefined settings.
        /// </summary>
        public Unattended(UnattendedSection settings)
        {
            setup(settings);
        }

        /// <summary>
        /// Setup the instance.
        /// </summary>
        /// <param name="settings">The settings to create the configuration from</param>
        private void setup(UnattendedSection settings)
        {
            updateManifests = new List<UpdateManifest>();

            log = LogManager.GetCurrentClassLogger();
            log.Info("Reading Update configurations...");

            clientId = settings.ClientID;

            string absoluteConfigPath = Path.GetFullPath(settings.ConfigurationDirectory);
            log.Info("Path to check for configs '{0}'", absoluteConfigPath);

            // Validation checks
            // Config directory
            if (Directory.Exists(absoluteConfigPath) == false)
            {
                log.Fatal("Configuration directory '{0}' does not exist or cannot be read from", absoluteConfigPath);
                throw new IOException("Configuration directory does not exist or cannot be read from: " + absoluteConfigPath);
            }

            // Validate the strategy from the config
            if (UpdateStrategy.IsValid(settings.Updates.Strategy) == false)
            {
                updateStrategy = UpdateStrategy.Default;
            }
            else updateStrategy = settings.Updates.Strategy;
            log.Info("Update strategy set as '{0}'", updateStrategy);

            // Validate the interval from the config
            if (UpdateInterval.IsValid(settings.Updates.Interval) == false)
            {
                updateInterval = UpdateInterval.Default;
            }
            else updateInterval = settings.Updates.Interval;
            log.Info("Update interval set as '{0}'", updateInterval);
            
            target = new Target(settings.Target);
            log.Info("Latest version directory is '{0}'", target.LatestVersionDirectory());
            
            // Load / Parse configs
            string[] configFiles = Directory.GetFiles(absoluteConfigPath, "*.uum", SearchOption.AllDirectories);
            foreach (string file in configFiles)
            {
                log.Debug("Check file '{0}'", Path.GetFileName(file));
                UpdateManifest manifest = UpdateManifest.FromFile(file);
                if (manifest != null)
                {
                    updateManifests.Add(manifest);
                }
            }
            log.Info("Loaded {0} update manifest{1}", updateManifests.Count, (updateManifests.Count == 1 ? "" : "s"));
        }

        /// <summary>
        /// Launch the target application 
        /// </summary>
        public void Start()
        {
            StartTargetApplication();

            // Always check for updates on startup
            ProcessUpdates();

            // Schedule the next update check time
            if (updateInterval == UpdateInterval.Daily)
            {
                PeriodicTask.Run(ProcessUpdates, TimeSpan.FromSeconds(30));
            }
            else if (updateInterval == UpdateInterval.Hourly)
            {
                PeriodicTask.Run(ProcessUpdates, TimeSpan.FromHours(1));
            }
            log.Info("Task created to check for updates");
            
            // Run the loop
            Run();
        }

        /// <summary>
        /// Main application loop.
        /// </summary>
        private void Run()
        {
            log.Info("Start main loop...");
            while (Console.KeyAvailable == false)
            {
                log.Debug("Loop update");
                ioCommand pingCommand = new ioCommand("Ping");
                ioServer.Execute(pingCommand);
                Thread.Sleep(1000);
            }

            ShutdownTargetApplication();
        }

        /// <summary>
        /// Start the target application using ioRPC
        /// </summary>
        private void StartTargetApplication()
        {
            // Launch the application and attach the ioRPC
            log.Info("Starting target application...");
            // Setup parameters for ioRPC
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = target.ApplicationPath;
            processInfo.Arguments = target.ApplicationParameters;
            
            ioServer = new Server(processInfo);
            // Add event handlers
            ioServer.CommandResultReceived += IoServer_CommandResultReceived;
            ioServer.CommandExceptionReceived += IoServer_CommandExceptionReceived;
            ioServer.Exited += IoServer_Exited;
            // Start the server
            ioServer.Start();
            log.Debug("Target application started");
        }

        /// <summary>
        /// Shuts down the target application.
        /// </summary>
        private void ShutdownTargetApplication()
        {
            log.Info("Shutting down target application...");
            // Send exit command
            ioCommand exitCommand = new ioCommand("Exit");
            ioServer.Execute(exitCommand);

            // Wait 5s then kill
            ioServer.ExitAndWait(5 * 1000);
            ioServer.CommandResultReceived -= IoServer_CommandResultReceived;
            ioServer.CommandExceptionReceived -= IoServer_CommandExceptionReceived;
            ioServer.Dispose();
            log.Debug("Target application shut down");
        }
        
        /// <summary>
        /// Check for updates, download and apply them if available
        /// </summary>
        private void ProcessUpdates()
        {
            // Not really needed as the task waits for the previous one
            // to complete before firing the delay. But rather be safe.
            lock (syncLock)
            {
                if (isUpdating == false)
                {
                    isUpdating = true;

                    List<OmahaManifest> omahaManifests;
                    omahaManifests = GetAvailableUpdates();
                    if (omahaManifests.Count > 0)
                    {
                        log.Info("{0} update{1} available", omahaManifests.Count, (omahaManifests.Count == 1 ? "" : "s"));

                        // Check if the update directory exists, if so, delete the it and its contents
                        string updatePath = target.BasePath + Path.DirectorySeparatorChar + "temp";
                        if (Directory.Exists(updatePath))
                        {
                            Directory.Delete(updatePath, true);
                        }
                        DirectoryInfo dir = Directory.CreateDirectory(updatePath);
                        Dictionary<OmahaManifest, string> downloadedPackages = new Dictionary<OmahaManifest, string>();
                        // Download the updates
                        foreach (OmahaManifest manifest in omahaManifests)
                        {
                            string downloadPath = updatePath + Path.DirectorySeparatorChar + manifest.Package.Name;
                            log.Info("Downloading update for {0} (Size {1} MB) from '{2}'", manifest.Package.Name, ((double)(manifest.Package.SizeInBytes) / 1024.00 / 1024.00).ToString("F5"), manifest.Url.Codebase);

                            WebClient webClient = new WebClient();
                            try
                            {
                                webClient.DownloadFile(manifest.Url.Codebase, downloadPath);
                            }
                            catch (WebException ex)
                            {
                                log.Warn("Download failed for {0} from '{1}': {2} ({3}). Will be retried at next interval.", 
                                    manifest.Package.Name, 
                                    manifest.Url.Codebase,
                                    ex.Status,
                                    ex.Message);
                                if (ex.InnerException != null)
                                {
                                    log.Warn("Download failed detail: {0}", ex.InnerException.Message);
                                }
                            }
                            log.Info("Download completed for {0}. Checking integrity.", manifest.Package.Name);
                            SHA256Managed sha = new SHA256Managed();
                            using (FileStream fileStream = new FileStream(downloadPath, FileMode.Open))
                            {
                                byte[] hashBytes = sha.ComputeHash(fileStream);
                                // Get the string of the hash
                                StringBuilder stringBuilder = new StringBuilder();
                                foreach (byte b in hashBytes)
                                {
                                    stringBuilder.AppendFormat("{0:x2}", b);
                                }
                                string hashString = stringBuilder.ToString();
                                if (hashString == manifest.Package.SHA256Hash)
                                {
                                    log.Info("Verified integrity of downloaded file for package {0}", manifest.Package.Name);
                                    downloadedPackages.Add(manifest, downloadPath);
                                }
                                else
                                {
                                    log.Error("Unable to verify integrity of downloaded file for package {0}. Download will be discarded and retried at next interval.", manifest.Package.Name);
                                }
                            }
                        }

                        log.Debug("Packages ready for updating: {0}", downloadedPackages.Count);

                        // Get the new version
                        KeyValuePair<DateTime, int>? currentVersion = target.CurrentVersion();
                        if (currentVersion == null)
                        {
                            currentVersion = new KeyValuePair<DateTime, int>(DateTime.MinValue, 0);
                        }
                        DateTime newDate = DateTime.Now;
                        int newCounter = 0;
                        // Check if the year-month-day strings match
                        if (currentVersion.Value.Key.ToString(target.VersionPathFormat) == newDate.ToString(target.VersionPathFormat))
                        {
                            newCounter = currentVersion.Value.Value + 1;
                        }
                        string newVersionDirectory = newDate.ToString(target.VersionPathFormat) + "." + newCounter;
                        newVersionDirectory = target.BasePath + Path.DirectorySeparatorChar + newVersionDirectory;
                        log.Debug("New version directory set as {0}", newVersionDirectory);

                        // Duplicate the current running version
                        try
                        {
                            DirectoryInfo directoryInfo = new DirectoryInfo(target.CurrentVersionDirectory());
                            directoryInfo.DeepCopyTo(newVersionDirectory);
                        }
                        catch (DirectoryNotFoundException ex)
                        {
                            log.Error("Unable to create new version: {0}", ex.Message);
                            isUpdating = false;
                            return;
                        }

                        // Extract the packages into the new version directory
                        foreach (KeyValuePair<OmahaManifest, string> kvp in downloadedPackages)
                        {
                            log.Trace("Extracting package {0}...", kvp.Key.Package.Name);
                            try
                            {
                                // Extract and overwrite
                                using(ZipArchive archive = ZipFile.OpenRead(kvp.Value))
                                { 
                                    foreach (ZipArchiveEntry entry in archive.Entries)
                                    {
                                        if (entry.Name == "" && entry.FullName != "" && entry.Length == 0)
                                        {
                                            Directory.CreateDirectory(newVersionDirectory + Path.DirectorySeparatorChar + entry.FullName);
                                        }
                                        else
                                        {
                                            ZipFileExtensions.ExtractToFile(entry, newVersionDirectory + Path.DirectorySeparatorChar + entry.FullName, true);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                log.Warn("Unable to extract package ({0}) to new version directory {1}: {2}", kvp.Key.Package.Name, newVersionDirectory, ex.Message);
                                continue;
                            }
                            log.Trace("Package {0} extracted", kvp.Key.Package.Name);
                        }

                        target.Update();
                        log.Info("Target updated. New application directory is '{0}'", target.ApplicationPath);

                        // Update the application by checking the update policy
                        if (updateStrategy == UpdateStrategy.Restart)
                        {
                            ShutdownTargetApplication();
                            StartTargetApplication();
                        }
                        else if (updateStrategy == UpdateStrategy.Prompt)
                        {
                            // if prompt, query and set state
                            ioCommand command = new ioCommand("CanUpdate");
                            ioServer.Execute(command);
                        }
                        
                        isUpdating = false;
                    }
                    else
                    {
                        log.Debug("No updates are available at this time");
                        isUpdating = false;
                    }
                }
                else
                {
                    // Do nothing
                    log.Trace("Update is already in progress");
                    return;
                }    
            }
        }
        
        /// <summary>
        /// Check for updates for the configured modules.
        /// </summary>
        /// <returns>A list of OmahaManifest's that require updates</returns>
        private List<OmahaManifest> GetAvailableUpdates()
        {
            log.Info("Checking for updates...");
            List<OmahaManifest> omahaManifests = new List<OmahaManifest>();
            foreach (UpdateManifest updateManifest in updateManifests)
            {
                OmahaManifest omahaManifest;
                if (IsUpdateAvailable(updateManifest, out omahaManifest) == true)
                {
                    log.Info("Update available for {0}. Version {1} ({2})", updateManifest.AppID, omahaManifest.Version, omahaManifest.TraceID);
                    omahaManifests.Add(omahaManifest);
                }
            }
            return omahaManifests;
        }

        /// <summary>
        /// Checks if a manifest requires an update.
        /// </summary>
        /// <param name="updateManifest">The manifest to check</param>
        /// <param name="omahaManifest">The Omaha update manifest, if result if true</param>
        /// <returns>true if the given updateManifest requires an update</returns>
        private bool IsUpdateAvailable(UpdateManifest updateManifest, out OmahaManifest omahaManifest)
        {
            omahaManifest = null;
            log.Debug("Checking update for application '{0}' at '{1}'", updateManifest.AppID, updateManifest.ServerUri);

            string version = "0.0.0.0";
            try
            {
                string currentVersionDirectory = Path.GetDirectoryName(target.ApplicationPath);
                AssemblyName assemblyName = AssemblyName.GetAssemblyName(currentVersionDirectory + Path.DirectorySeparatorChar + updateManifest.AppPath);
                version = assemblyName.Version.ToString();
                log.Trace("{0} is at assembly version {1}", updateManifest.AppID, version);
            }
            catch (Exception ex)
            {
                log.Warn("Unable to get assembly version for '{0}': {1}", updateManifest.AppID, ex.Message);
                return false;
            }
            
            // Create the data to send to the server
            OmahaRequest request = new OmahaRequest();
            request.Application.ClientID = clientId;
            request.Application.ID = updateManifest.AppID;
            request.Application.Version = version;
            request.Application.Event.EventType = OmahaEventType.UpdateCheck;
            request.Application.Event.EventResult = OmahaEventResultType.Started;

            XmlSerializer serializer = new XmlSerializer(request.GetType());
            byte[] serializedRequest;
            // TODO: Clean up the two usings
            using (MemoryStream memoryStream = new MemoryStream())
            {
                serializer.Serialize(memoryStream, request);
                serializedRequest = memoryStream.ToArray();
            }
            
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", string.Format("Limitless Unattended ({0})", version));
                try
                {
                    //log.Debug("Data to be sent to update server: {0}", Encoding.UTF8.GetString(serializedRequest));
                    var response = client.UploadData(updateManifest.ServerUri, "POST", serializedRequest);
                    
                    string responseString = Encoding.Default.GetString(response);
                    //log.Debug("Response from update server: {0}", responseString);

                    OmahaResponse omahaResponse = null;
                    XmlSerializer parser = new XmlSerializer(typeof(OmahaResponse));
                    try
                    {
                        omahaResponse = (OmahaResponse)parser.Deserialize(new StringReader(responseString));
                        if (omahaResponse.Application.Status == "ok")
                        {
                            if (omahaResponse.Application.UpdateCheck.Status == "ok")
                            {
                                omahaManifest = omahaResponse.Application.UpdateCheck.Manifest;
                                return true;
                            }
                            else if (omahaResponse.Application.UpdateCheck.Status == "noupdate")
                            {
                                log.Debug("No update available yet for {0}. At version {1}", request.Application.ID, request.Application.Version);
                                return false;
                            }
                        }
                        else
                        {
                            log.Warn("The update server responded with App Status {0}: '{1}'", omahaResponse.Application.Status, omahaResponse.Application.Reason);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Warn("Unable to parse Omaha response for application '{0}': {1}", request.Application.ID, ex.Message);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    log.Warn("Unable to check updates for application {0}: '{1}'", request.Application.ID, ex.Message);
                    return false;
                }
            }
            return false;
        }
        
        #region Event Handlers
        private void IoServer_Exited(object sender, EventArgs e)
        {
            log.Info("ioRPC - Client exited");
        }

        private void IoServer_CommandExceptionReceived(object sender, ioRPC.Events.ioEventArgs e)
        {
            log.Info("ioRPC - Client Exception: '{0} - {1}'", e.CommandName, e.ExceptionMessage);
        }

        private void IoServer_CommandResultReceived(object sender, ioRPC.Events.ioEventArgs e)
        {
            log.Info("ioRPC - Client Result: '{0} - {1}'", e.CommandName, e.Data);
            if (e.CommandName == "CanUpdate")
            {
                bool canUpdate = (bool)e.Data;
                if (canUpdate)
                {
                    ShutdownTargetApplication();
                    StartTargetApplication();
                }
            }
        }
        #endregion
    }
}
