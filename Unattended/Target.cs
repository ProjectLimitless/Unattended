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
using System.IO;

using Limitless.Unattended.Configuration;
using System.Collections.Generic;
using System;
using System.Globalization;

namespace Limitless.Unattended.Structs
{
    /// <summary>
    /// The target client application and everything related to it.
    /// </summary>
    public class Target
    {
        /// <summary>
        /// The filename of the target application.
        /// </summary>
        public string ApplicationFilename { get; private set; }
        /// <summary>
        /// The target application's full path.
        /// </summary>
        public string ApplicationPath { get; private set; }
        /// <summary>
        /// Parameters to pass to the target application.
        /// </summary>
        public string ApplicationParameters { get; private set; }
        /// <summary>
        /// Absolute path to the target application's root path.
        /// </summary>
        public string BasePath { get; private set; }
        /// <summary>
        /// Defines the date path format for application directories. Defaults to yyyyMMdd
        /// </summary>
        public string VersionPathFormat { get; private set; }

        /// <summary>
        /// Create a target application with the given settings.
        /// </summary>
        /// <param name="settings">The settings from the configuration</param>
        public Target(TargetSection settings)
        {
            VersionPathFormat = "yyyyMMdd";
            setup(settings);
        }

        /// <summary>
        /// Create a target application with the given settings and version path format.
        /// </summary>
        /// <param name="settings">The settings from the configuration</param>
        /// <param name="versionPathFormat">The DateTime format of version paths</param>
        public Target(TargetSection settings, string versionPathFormat)
        {
            VersionPathFormat = versionPathFormat;
            setup(settings);
        }

        /// <summary>
        /// Get the directory of the latest application version.
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        /// <returns>The path to the directory</returns>
        public string LatestVersionDirectory()
        {
            string[] applicationDirectories = Directory.GetDirectories(BasePath, "*", SearchOption.TopDirectoryOnly);

            string latestVersionDirectory = "";
            KeyValuePair<DateTime, int> latestVersion = new KeyValuePair<DateTime, int>(DateTime.MinValue, 0);
            foreach (string applicationDirectory in applicationDirectories)
            {
                KeyValuePair<DateTime, int>? version = ParseVersionDirectory(applicationDirectory);
                if (version == null)
                {
                    // Directory does not conform to update folder specifications
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
                throw new NotSupportedException("No available version directory found");
            }
            return latestVersionDirectory;
        }

        /// <summary>
        /// Gets the current version's directory.
        /// </summary>
        /// <returns>The full path to the current version</returns>
        public string CurrentVersionDirectory()
        {
            return Path.GetDirectoryName(ApplicationPath);
        }

        /// <summary>
        /// Get the current version based on the directory.
        /// </summary>
        /// <returns>The version information, or null</returns>
        public KeyValuePair<DateTime, int>? CurrentVersion()
        {
            return ParseVersionDirectory(CurrentVersionDirectory());
        }

        /// <summary>
        /// Rechecks versions to update to the latest executable.
        /// </summary>
        public void Update()
        {
            // Check if the target application exists in the latest verion path
            ApplicationPath = LatestVersionDirectory() + Path.DirectorySeparatorChar + ApplicationFilename;
            if (File.Exists(ApplicationPath) == false)
            {
                throw new IOException("Target application does not exist or cannot be read from: " + ApplicationPath);
            }
        }

        /// <summary>
        /// Setup parameters from the config.
        /// </summary>
        /// <param name="settings">The settings from the configuration</param>
        private void setup(TargetSection settings)
        {
            // Check base path
            BasePath = Path.GetFullPath(settings.BasePath);
            if (Directory.Exists(BasePath) == false)
            {
                throw new IOException("Application root directory does not exist at " + BasePath);
            }

            ApplicationParameters = "";
            if (settings.Parameters != null)
            {
                ApplicationParameters = settings.Parameters;
            }

            // Check if the target application exists in the latest verion path
            ApplicationFilename = settings.Filename;
            ApplicationPath = LatestVersionDirectory() + Path.DirectorySeparatorChar + ApplicationFilename;
            if (File.Exists(ApplicationPath) == false)
            {
                throw new IOException("Target application does not exist or cannot be read from: " + ApplicationPath);
            }
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
                        VersionPathFormat,
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
    }
}
