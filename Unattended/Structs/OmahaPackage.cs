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
    /// The package tag section
    /// </summary>
    [XmlRoot("package")]
    public class OmahaPackage
    {
        /// <summary>
        /// The SHA256 hash of the download file.
        /// </summary>
        [XmlAttribute("hash")]
        public string SHA256Hash { get; set; }
        /// <summary>
        /// The name of the download file.
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }
        /// <summary>
        /// The size in bytes of the download file.
        /// </summary>
        [XmlAttribute("size")]
        public int SizeInBytes { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public OmahaPackage()
        {
            SHA256Hash = "";
            Name = "";
            SizeInBytes = 0;
        }
    }
}
