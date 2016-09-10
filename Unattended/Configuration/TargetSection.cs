

using System;
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
namespace Limitless.Unattended.Configuration
{
    /// <summary>
    /// Holds configuration information for unattended's target application.
    /// </summary>
    public class TargetSection
    {
        /// <summary>
        /// Path to the installation root of the application.
        /// Defaults to current directory.
        /// </summary>
        public string BasePath { get; set; }
        /// <summary>
        /// The filename of the target.
        /// Defaults to 'App.exe';
        /// </summary>
        public string Filename { get; set; }
        /// <summary>
        /// The parameters to pass to the application.
        /// Default to blank.
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TargetSection()
        {
            BasePath = Environment.CurrentDirectory;
            Filename = "App.exe";
            Parameters = "";
        }
    }
}