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

namespace Limitless.Unattended
{
    /// <summary>
    /// Unattended core, runs the update lifecycle.
    /// </summary>
    public class Unattended
    {
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
        /// Absolute path to the target application's root path.
        /// </summary>
        private string basePath;
        /// <summary>
        /// The name of the target application.
        /// </summary>
        private string applicationFilename;
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
            updateStrategy = getValidUpdateStrategy(settings.Updates.Strategy);
            log.Info("Update strategy set as '{0}'", updateStrategy);
            
            // 3. Validate the interval
            updateInterval = getValidUpdateInterval(settings.Updates.Interval);
            log.Info("Update interval set as '{0}'", updateInterval);

            // 4. Check base path
            basePath = Path.GetFullPath(settings.Target.BasePath);
            if (Directory.Exists(basePath) == false)
            {
                log.Fatal("Application root directory does not exist at '{0}'", basePath);
                throw new IOException("Application root directory does not exist at " + basePath);
            }

            // 5. Determine the latest version of the application
            KeyValuePair<DateTime, int> latestVersion = new KeyValuePair<DateTime, int>(DateTime.MinValue, 0);
            log.Debug("Getting available directories in basepath '{0}'", basePath);
            string[] applicationDirectories = Directory.GetDirectories(basePath, "*", SearchOption.TopDirectoryOnly);
            foreach (string applicationDirectory in applicationDirectories)
            {
                log.Debug("Directory in application root path: '{0}'", applicationDirectory);
                string directoryName = applicationDirectory.Substring(applicationDirectory.LastIndexOf(Path.DirectorySeparatorChar));
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
                            log.Debug(" Path has date set as: '{0}'", datePart.ToString("yyyy-MM-dd"));
                            // Check if parts[1] is a counter value
                            int counter;
                            bool counterParsed = Int32.TryParse(parts[1], out counter);
                            if (counterParsed == true)
                            {
                                log.Debug(" Path has counter value of: '{0}'", counter);

                                if (datePart > latestVersion.Key)
                                {
                                    latestVersion = new KeyValuePair<DateTime, int>(datePart, counter);
                                } 
                                else if (datePart == latestVersion.Key)
                                {
                                    if (counter > latestVersion.Value)
                                    {
                                        // Dates are the same, check counter
                                        latestVersion = new KeyValuePair<DateTime, int>(datePart, counter);
                                    }
                                }
                            }
                        }
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

            //


            /*
            // 5. Check if the target application exists
            if (Directory.Exists(basePath) == false)
            {
                log.Fatal("Target application '{0}' does not exist or cannot be read from", applicationPath);
                throw new IOException("Target application does not exist or cannot be read from: " + applicationPath);
            }
            log.Info("Target application is '{0}'", Path.GetFileName(applicationPath));
            */

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
            /*ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = applicationPath;
            processInfo.Arguments = applicationParameters;

            ioServer = new Server(processInfo);
            // Add event handlers
            ioServer.CommandResultReceived += IoServer_CommandResultReceived;
            ioServer.CommandExceptionReceived += IoServer_CommandExceptionReceived;
            ioServer.Exited += IoServer_Exited;
            // Start the server
            ioServer.Start();

            // Set the update check time
            // TODO: This should move and be handled in the loop?
            List<OmahaManifest> omahaManifests;
            if (updateInterval == UpdateIntervals.Startup)
            {
                log.Info("Application started, check for updates...");
                omahaManifests = CheckUpdates();
                if (omahaManifests.Count > 0)
                {
                    log.Info("{0} update{1} available", omahaManifests.Count, (omahaManifests.Count == 1 ? "" : "s"));
                }
            }
            else if (updateInterval == UpdateIntervals.Daily)
            {
                log.Info("Daily interval set for one hour from now...");
            }

            //TODO: Create the secondary path / backup to the alternate path?
            //TODO: Process the list of updates

            // Run the loop
            Run();*/
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
            ioServer.ExitAndWait(5000);
            ioServer.Dispose();
        }

        /// <summary>
        /// Check for updates for the configured pieces.
        /// </summary>
        /// <returns>A list of OmahaManifest's that require updates</returns>
        private List<OmahaManifest> CheckUpdates()
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
            request.Application.Event.EventType = OmahaEventTypes.UpdateCheck;
            request.Application.Event.EventResult = OmahaEventResultTypes.Started;

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
                    log.Debug("Data to be sent to update server: {0}", Encoding.UTF8.GetString(serializedRequest));
                    var response = client.UploadData(updateManifest.ServerUri, "POST", serializedRequest);
                    
                    string responseString = Encoding.Default.GetString(response);
                    log.Debug("Response from update server: {0}", responseString);

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
        private string getValidUpdateStrategy(string checkStrategy)
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
        private string getValidUpdateInterval(string checkInterval)
        {
            string interval = "";
            switch (checkInterval.ToLower())
            {
                case UpdateIntervals.Startup:
                    interval = UpdateIntervals.Startup;
                    break;
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
