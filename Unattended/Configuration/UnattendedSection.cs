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
    /// Holds configuration information for unattended.
    /// </summary>
    public class UnattendedSection
    {
        /// <summary>
        /// The unique client ID for this installation.
        /// </summary>
        public string ClientID { get; set; }
        /// <summary>
        /// The directory to read multiple update configurations from.
        /// </summary>
        public string ConfigurationDirectory { get; set; }
        /// <summary>
        /// The update policies
        /// </summary>
        public UpdatesSection Updates { get; set; }
        /// <summary>
        /// The application to manage and wrap around.
        /// </summary>
        public TargetSection Target{ get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public UnattendedSection()
        {
            ClientID = "defaultclient";
            ConfigurationDirectory = "configurations";
            Target = new TargetSection();
            Updates = new UpdatesSection();
        }
    }
}