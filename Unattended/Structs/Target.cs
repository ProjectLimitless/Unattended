
using Limitless.Unattended.Configuration;
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
using System.Xml.Serialization;

namespace Limitless.Unattended.Structs
{
    /// <summary>
    /// The target client application and everything related to it.
    /// </summary>
    public class Target
    {
        /// <summary>
        /// The target application.
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
        }
    }
}
