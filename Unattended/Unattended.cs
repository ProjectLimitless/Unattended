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
using NLog;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using Limitless.Unattended.Structs;
using Limitless.Unattended.Configuration;
using System.Reflection;
using System.Net;
using Limitless.Unattended.Enums;

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
        /// Collection of the updates to check at the interval.
        /// </summary>
        private List<UpdateManifest> updateManifests;

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

            string absoluteAppPath = Path.GetFullPath(settings.Target.Path);
            // 4. Check if the target application exists
            if (File.Exists(absoluteAppPath) == false)
            {
                log.Fatal("Target application '{0}' does not exist or cannot be read from", absoluteAppPath);
                throw new IOException("Target application does not exist or cannot be read from: " + absoluteAppPath);
            }
            log.Info("Target application is '{0}'", Path.GetFileName(absoluteAppPath));

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

            // Set the update check time
            if (updateInterval == UpdateIntervals.Startup)
            {
                log.Info("Application started, check for updates...");
                CheckUpdates();
            }
            else if (updateInterval == UpdateIntervals.Daily)
            {
                log.Info("Daily interval set for one hour from now...");
            }
        }

        /// <summary>
        /// Check for updates for the configured pieces.
        /// </summary>
        private void CheckUpdates()
        {
            log.Info("Checking for updates...");
            foreach (UpdateManifest updateManifest in updateManifests)
            {
                CheckUpdate(updateManifest);
            }
        }

        /// <summary>
        /// Checks if a manifest requires and update.
        /// </summary>
        /// <param name="updateManifest">The manifest to check</param>
        private bool CheckUpdate(UpdateManifest updateManifest)
        {
            log.Debug("Checking update for application '{0}' at '{1}'", updateManifest.AppID, updateManifest.ServerUri);

            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            WebRequest webRequest = WebRequest.Create(updateManifest.ServerUri);
            ((HttpWebRequest)webRequest).Method = "POST";
            ((HttpWebRequest)webRequest).UserAgent = string.Format("Limitless Unattended ({0})", version);

            // Create the data to send to the server
            OmahaRequest request = new OmahaRequest();
            request.Application.ClientID = clientId;
            request.Application.ID = updateManifest.AppID;
            request.Application.Version = version;
            request.Application.Event.EventType = OmahaEventTypes.UpdateCheck;
            request.Application.Event.EventResult = OmahaEventResultTypes.Started;

            XmlSerializer serializer = new XmlSerializer(request.GetType());
            string serializedRequest = "";
            using (StringWriter textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, request);
                serializedRequest = textWriter.ToString();
            }
            Console.WriteLine(serializedRequest);

            /*
            string remoteManifestData = "";
            log.Debug("Fetching remote manifest data...");
            try
            {
                WebResponse response = request.GetResponse();
                HttpStatusCode statusCode = ((HttpWebResponse)response).StatusCode;
                log.Debug("Got response from Uri: {0}", statusCode.ToString());
                if (statusCode == HttpStatusCode.OK)
                {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    remoteManifestData = reader.ReadToEnd();
                    reader.Close();
                }
                response.Close();
            }
            catch (WebException ex)
            {
                log.Error("Unable to fetch remote manifest at {0}: {1}", manifest.Uri, ex.Message);
                return false;
            }

            // Parse the remote manifest
            RemoteManifest remoteManifest;
            XmlSerializer parser = new XmlSerializer(typeof(RemoteManifest));
            try
            {
                remoteManifest = (RemoteManifest)parser.Deserialize(new StringReader(remoteManifestData));
            }
            catch (Exception ex)
            {
                log.Error("Unable to parse remote manifest: '{0}'", ex.Message);
                return false;
            }
            // Check the manifest
            if (remoteManifest == null)
            {
                log.Error("The remote manifest is null: '{0}'", manifest.Name);
                return false;
            }
            if (remoteManifest.Name == string.Empty)
            {
                log.Warn("The remote manifest does not contain the file name: '{0}'", manifest.Name);
                return false;
            }


            // Check if update is required
            switch (manifest.Policy)
            {
                case Policy.FileVersion:
                    // Get the current file version.
                    Version currentVersion = AssemblyName.GetAssemblyName(manifest.File).Version;
                    log.Debug("Current file version is {0}", currentVersion.ToString());
                    Version remoteVersion;
                    if (Version.TryParse(remoteManifest.Version, out remoteVersion))
                    {
                        log.Debug("Parsed remote manifest version as {0}", remoteVersion);
                        if (currentVersion.CompareTo(remoteVersion) < 0)
                        {
                            log.Info("'{0}' requires an update, remote version is newer.", Path.GetFileName(manifest.File));
                            updateInfo = new KeyValuePair<Manifest, RemoteManifest>(manifest, remoteManifest);
                            return true;
                        }
                    }
                    else
                    {
                        log.Warn("Unable to parse remote manifest version '{0}'", remoteManifest.Version);
                    }
                    break;
            }
            */
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
    }
}
