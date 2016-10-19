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

using System.Xml.Serialization;

namespace Limitless.Unattended.Structs
{
    /// <summary>
    /// The configuration for parts that need to be updated.
    /// </summary>
    [XmlRoot("UpdateManifest")]
    public class UpdateManifest
    {
        /// <summary>
        /// The globally unique application ID as assigned by the update server. This 
        /// also applies to library files, not just executables.
        /// </summary>
        public string AppID { get; set; }
        /// <summary>
        /// The path to the application to check version of and update.
        /// It can be any .NET versionable file like an exe and dll.
        /// </summary>
        public string AppPath { get; set; }
        /// <summary>
        /// The URI of the update server.
        /// </summary>
        public string ServerUri { get; set; }
    }
}
