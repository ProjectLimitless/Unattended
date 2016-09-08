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
    /// The manifest tag section
    /// </summary>
    [XmlRoot("manifest")]
    public class OmahaManifest
    {
        /// <summary>
        /// The version of the update;
        /// </summary>
        [XmlAttribute("version")]
        public string Version { get; set; }
        /// <summary>
        /// The unique trace for this response and following update requests.
        /// </summary>
        [XmlAttribute("trace")]
        public string TraceID { get; set; }
        /// <summary>
        /// The url to download from.
        /// </summary>
        [XmlElement("url")]
        public OmahaUrl Url { get; set; }
        /// <summary>
        /// Validation information for the download.
        /// </summary>
        [XmlElement("package")]
        public OmahaPackage Package { get; set; }
        /// <summary>
        /// Default constructor.
        /// </summary>
        public OmahaManifest()
        {
            Url = new OmahaUrl();
            Package = new OmahaPackage();
        }
    }
}
