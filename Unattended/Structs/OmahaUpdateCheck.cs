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
using Limitless.Unattended.Enums;

namespace Limitless.Unattended.Structs
{
    /// <summary>
    /// The updatecheck tag section
    /// </summary>
    [XmlRoot("updatecheck")]
    public class OmahaUpdateCheck
    {
        /// <summary>
        /// The status of the update check.
        /// </summary>
        [XmlAttribute("status")]
        public string Status { get; set; }
        /// <summary>
        /// The update manifest.
        /// </summary>
        [XmlElement("manifest")]
        public OmahaManifest Manifest { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public OmahaUpdateCheck()
        {
            Status = "ok";
            Manifest = new OmahaManifest();
        }
    }
}
