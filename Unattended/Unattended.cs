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
using System.Configuration;
using System.Xml.Serialization;
using System.Collections.Generic;
using Limitless.Unattended.Structs;
using Limitless.Unattended.Configuration;

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
        /// Standard constructor.
        /// </summary>
        public Unattended()
        {
            updateManifests = new List<UpdateManifest>();

            log = LogManager.GetCurrentClassLogger();
            log.Info("Reading Update configurations...");
            
            var settings = (UnattendedSection)(dynamic)ConfigurationManager.GetSection("unattended");

            string absoluteConfigPath = Path.GetFullPath(settings.ConfigurationDirectory);
            log.Info("Path to check for configs '{0}'", absoluteConfigPath);

            // Validation checks
            // 1. Config directory
            if (Directory.Exists(absoluteConfigPath) == false)
            {
                log.Fatal("Configuration directory '{0}' does not exist or cannot be read from", absoluteConfigPath);
                //TODO: Add back - throw new IOException("Configuration directory does not exist or cannot be read from");
            }

            // 2. Validate the strategy
            updateStrategy = getValidUpdateStrategy(settings.Updates.Strategy);
            log.Info("Update strategy set as '{0}'", updateStrategy);
            // 3. Validate the interval
            updateInterval = getValidUpdateInterval(settings.Updates.Interval);
            log.Info("Update interval set as '{0}'", updateInterval);

            // 4. Check if the target application exists
            if (File.Exists(settings.Target.Path) == false)
            {
                log.Fatal("Target application '{0}' does not exist or cannot be read from", settings.Target.Path);
                //TODO: Add back - throw new IOException("Target application '{0}' does not exist or cannot be read from");
            }
            log.Info("Target application is '{0}'", Path.GetFileName(settings.Target.Path));

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
        /// Start the check and update loop.
        /// </summary>
        public void Run()
        {
            
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
