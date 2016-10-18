﻿/** 
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
using System.Reflection;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Collections.Generic;

using NLog;
using Limitless.ioRPC;
using Limitless.Unattended.Enums;
using Limitless.Unattended.Structs;
using Limitless.Unattended.Configuration;
using Limitless.ioRPC.Structs;
using System.Threading;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.IO.Compression;

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
        /// Flag to check if we are currently in update state.
        /// </summary>
        private volatile bool isUpdating;
        /// <summary>
        /// Absolute path to the target application's root path.
        /// </summary>
        private string basePath;
        /// <summary>
        /// The target application.
        /// </summary>
        private string applicationPath;
        /// <summary>
        /// Parameters to pass to the target application.
        /// </summary>
        private string applicationParameters;
        /// <summary>
        /// Collection of the updates to check at the interval.
        /// </summary>
        private List<UpdateManifest> updateManifests;
        /// <summary>
        /// ioRPC server instance.
        /// </summary>
        private Server ioServer;
        /// <summary>
        /// Defines the date path format for application directories. Defaults to yyyyMMdd
        /// </summary>
        private string applicationPathDateFormat;

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
            applicationPathDateFormat = "yyyyMMdd";
            updateManifests = new List<UpdateManifest>();

            log = LogManager.GetCurrentClassLogger();
            log.Info("Reading Update configurations...");

            clientId = settings.ClientID;

            string absoluteConfigPath = Path.GetFullPath(settings.ConfigurationDirectory);
            log.Info("Path to check for configs '{0}'", absoluteConfigPath);

            // Validation checks
            // 1. Config directory
            if (Directory.Exists(absoluteConfigPath) == false)
            {
                log.Fatal("Configuration directory '{0}' does not exist or cannot be read from", absoluteConfigPath);
                throw new IOException("Configuration directory does not exist or cannot be read from: " + absoluteConfigPath);
            }

            // 2. Validate the strategy
            updateStrategy = GetValidUpdateStrategy(settings.Updates.Strategy);
            log.Info("Update strategy set as '{0}'", updateStrategy);
            
            // 3. Validate the interval
            updateInterval = GetValidUpdateInterval(settings.Updates.Interval);
            log.Info("Update interval set as '{0}'", updateInterval);

            // 4. Check base path
            basePath = Path.GetFullPath(settings.Target.BasePath);
            if (Directory.Exists(basePath) == false)
            {
                log.Fatal("Application root directory does not exist at '{0}'", basePath);
                throw new IOException("Application root directory does not exist at " + basePath);
            }

            // 5. Determine the latest version of the application
            log.Debug("Getting available directories in basepath '{0}'", basePath);
            string[] applicationDirectories = Directory.GetDirectories(basePath, "*", SearchOption.TopDirectoryOnly);
            string latestVersionDirectory = GetLatestVersionDirectory(applicationDirectories);
            log.Info("Latest version directory is '{0}'", latestVersionDirectory);

            // 6. Check if the target application exists in the latest verion path
            applicationPath = latestVersionDirectory + Path.DirectorySeparatorChar + settings.Target.Filename;
            log.Info("Target application is at '{0}'", applicationPath);
            if (File.Exists(applicationPath) == false)
            {
                log.Fatal("Target application '{0}' does not exist or cannot be read from", applicationPath);
                throw new IOException("Target application does not exist or cannot be read from: " + applicationPath);
            }
            
            applicationParameters = "";
            if (settings.Target.Parameters != null)
            {
                applicationParameters = settings.Target.Parameters;
            }

            // Load / Parse configs
            string[] configFiles = Directory.GetFiles(absoluteConfigPath, "*.uum", SearchOption.AllDirectories);
            foreach (string file in configFiles)
            {
                log.Debug("Check file '{0}'", Path.GetFileName(file));
                UpdateManifest manifest = LoadUpdateManifest(file);
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
            // Launch the application and attach the ioRPC
            log.Info("Starting target application...");
            // Setup parameters for ioRPC
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = applicationPath;
            processInfo.Arguments = applicationParameters;

            ioServer = new Server(processInfo);
            // Add event handlers
            ioServer.CommandResultReceived += IoServer_CommandResultReceived;
            ioServer.CommandExceptionReceived += IoServer_CommandExceptionReceived;
            ioServer.Exited += IoServer_Exited;
            // Start the server
            ioServer.Start();

            // Always check for updates on startup
            ProcessUpdates();

            // Schedule the next update check time
            if (updateInterval == UpdateIntervals.Daily)
            {
                PeriodicTask.Run(ProcessUpdates, TimeSpan.FromDays(1));
            }
            else if (updateInterval == UpdateIntervals.Hourly)
            {
                PeriodicTask.Run(ProcessUpdates, TimeSpan.FromHours(1));
            }
            log.Info("Task created to check for updates");

            //TODO: Create the secondary path / backup to the alternate path?



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
                Thread.Sleep(1000);
            }
            // Send exit command
            ioCommand exitCommand = new ioCommand("Exit");
            ioServer.Execute(exitCommand);

            // Wait 5s then kill
            ioServer.ExitAndWait(5 * 1000);
            ioServer.Dispose();
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
                        string updatePath = basePath + Path.DirectorySeparatorChar + "temp";
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
                                    log.Info("Verified intergrity of downloaded file for package {0}", manifest.Package.Name);
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
                        string currentVersionDirectory = Path.GetDirectoryName(applicationPath);
                        KeyValuePair<DateTime, int>? currentVersion = ParseVersionDirectory(currentVersionDirectory);
                        if (currentVersion == null)
                        {
                            currentVersion = new KeyValuePair<DateTime, int>(DateTime.MinValue, 0);
                        }
                        DateTime newDate = DateTime.Now;
                        int newCounter = 0;
                        // Check if the year-month-day strings match
                        if (currentVersion.Value.Key.ToString("yyyyMMdd") == newDate.ToString("yyyyMMdd"))
                        {
                            newCounter = currentVersion.Value.Value + 1;
                        }
                        string newVersionDirectory = newDate.ToString("yyyyMMdd") + "." + newCounter;
                        newVersionDirectory = basePath + Path.DirectorySeparatorChar + newVersionDirectory;
                        log.Debug("New version directory set as {0}", newVersionDirectory);

                        // Duplicate the current running version
                        try
                        { 
                            DirectoryCopy(currentVersionDirectory, newVersionDirectory, true);
                        }
                        catch (Exception ex)
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
                                ZipArchive archive = ZipFile.OpenRead(kvp.Value);
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
                            catch (Exception ex)
                            {
                                log.Warn("Unable to extract package ({0}) to new version directory {1}: {2}", kvp.Key.Package.Name, newVersionDirectory, ex.Message);
                                continue;
                            }
                            log.Trace("Package {0} extracted", kvp.Key.Package.Name);
                        }

                        // Update the application by checking the update policy
                        // if off, do nothing - TODO: Remove off?
                        // if restart, do so now
                        // if prompt, query and set state

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
                if (UpdateAvailable(updateManifest, out omahaManifest) == true)
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
        private bool UpdateAvailable(UpdateManifest updateManifest, out OmahaManifest omahaManifest)
        {
            omahaManifest = null;
            log.Debug("Checking update for application '{0}' at '{1}'", updateManifest.AppID, updateManifest.ServerUri);

            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
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

        /// <summary>
        /// Loads and parses an update configuration manifest.
        /// </summary>
        /// <param name="path">The path to the manifest</param>
        /// <returns>The parsed update manifest</returns>
        private UpdateManifest LoadUpdateManifest(string path)
        {
            UpdateManifest manifest = null;
            XmlSerializer parser = new XmlSerializer(typeof(UpdateManifest));
            try
            {
                manifest = (UpdateManifest)parser.Deserialize(new StreamReader(path));
            }
            catch (Exception ex)
            {
                log.Fatal("Unable to parse update manifest '{0}': {1}", Path.GetFileName(path), ex.Message);
                throw;
            }
            return manifest;
        }

        /// <summary>
        /// Checks if the given checkStrategy is valid and returns the valid value.
        /// </summary>
        /// <param name="checkStrategy">The desired strategy</param>
        /// <returns>checkStrategy if valid, the default 'restart' otherwise</returns>
        private string GetValidUpdateStrategy(string checkStrategy)
        {
            string strategy = "";
            switch (checkStrategy.ToLower())
            {
                case UpdateStrategies.Prompt:
                    strategy = UpdateStrategies.Prompt;
                    break;
                case UpdateStrategies.Restart:
                    strategy = UpdateStrategies.Restart;
                    break;
                case UpdateStrategies.Off:
                    strategy = UpdateStrategies.Off;
                    break;
                default:
                    log.Warn("The update strategy '{0}' is not valid, will default to using 'restart'", checkStrategy);
                    strategy = UpdateStrategies.Restart;
                    break;
            }
            return strategy;
        }

        /// <summary>
        /// Checks if the given checkInterval is valid and returns the valid value.
        /// </summary>
        /// <param name="checkInterval">The desired interval</param>
        /// <returns>checkInterval if valid, the default 'daily' otherwise</returns>
        private string GetValidUpdateInterval(string checkInterval)
        {
            string interval = "";
            switch (checkInterval.ToLower())
            {
                case UpdateIntervals.Daily:
                    interval = UpdateIntervals.Daily;
                    break;
                default:
                    log.Warn("The update interval '{0}' is not valid, will default to using 'daily'", checkInterval);
                    interval = UpdateIntervals.Daily;
                    break;
            }
            return interval;
        }

        /// <summary>
        /// Get's the latest version application path from the list of given directories.
        /// </summary>
        /// <param name="applicationDirectories">The list of available application paths</param>
        /// <returns>The latest version path</returns>
        private string GetLatestVersionDirectory(string[] applicationDirectories)
        {
            string latestVersionDirectory = "";
            KeyValuePair<DateTime, int> latestVersion = new KeyValuePair<DateTime, int>(DateTime.MinValue, 0);            
            foreach (string applicationDirectory in applicationDirectories)
            {
                log.Debug("Directory in application root path: '{0}'", applicationDirectory);
                KeyValuePair<DateTime, int>? version = ParseVersionDirectory(applicationDirectory);
                if (version == null)
                {
                    log.Debug("Directory '{0}' does not conform to update folder specifications", applicationDirectory);
                    continue;
                }

                if (version.Value.Key > latestVersion.Key)
                {
                    latestVersion = version.Value;
                    latestVersionDirectory = applicationDirectory;
                }
                else if (version.Value.Key == latestVersion.Key)
                {
                    if (version.Value.Value > latestVersion.Value)
                    {
                        // Dates are the same, check counter
                        latestVersion = version.Value;
                        latestVersionDirectory = applicationDirectory;
                    }
                }
            }
            if (latestVersion.Key == DateTime.MinValue)
            {
                // No versions found
                log.Fatal("No available version directory found.");
                throw new NotSupportedException("No available version directory found");
            }
            log.Info("Latest version is '{0}.{1}'", latestVersion.Key.ToString(applicationPathDateFormat), latestVersion.Value);
            return latestVersionDirectory;
        }

        /// <summary>
        /// Parses directoryName and returns the KeyValuePair result or null.
        /// </summary>
        /// <param name="directoryName">The name of the directory to parse</param>
        /// <returns>The parsed version information or null</returns>
        private KeyValuePair<DateTime, int>? ParseVersionDirectory(string directoryName)
        {
            directoryName = directoryName.Substring(directoryName.LastIndexOf(Path.DirectorySeparatorChar));
            directoryName = directoryName.Replace(Path.DirectorySeparatorChar.ToString(), "");
            // Check if the path conforms to the date.counter format
            // but first, check if there is a '.' for date.counter format
            // The actual format is 20161231.1
            if (directoryName.Contains(".") == true)
            {
                // parts[0] should be the date and parts[1] should be the counter
                string[] parts = directoryName.Split('.');
                if (parts.Length == 2)
                {
                    DateTime datePart;
                    bool dateParsed = DateTime.TryParseExact(
                        parts[0],
                        applicationPathDateFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out datePart);
                    if (dateParsed == true)
                    {
                        int counter;
                        bool counterParsed = Int32.TryParse(parts[1], out counter);
                        if (counterParsed == true)
                        {
                            return new KeyValuePair<DateTime, int>(datePart, counter);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Copy a directory and all its contents from sourceDirName to destDirName.
        /// This is taken from MSDN. https://msdn.microsoft.com/en-us/library/bb762914(v=vs.110).aspx
        /// </summary>
        /// <param name="sourceDirName">The source directory</param>
        /// <param name="destDirName">The destination directory</param>
        /// <param name="copySubDirs">If true, all subdirectories and their contents will also  be copied</param>
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
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
        }
        #endregion
    }
}
