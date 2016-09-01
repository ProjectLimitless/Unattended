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

namespace Limitless.Unattended.Configuration
{
    /// <summary>
    /// Holds configuration information for unattended's updating policies.
    /// </summary>
    public class UpdatesSection
    {
        /// <summary>
        /// What to do when an update is ready.
        /// </summary>
        public string Strategy { get; set; }
        /// <summary>
        /// When to check for updates.
        /// </summary>
        public string Interval { get; set; }
    }
}