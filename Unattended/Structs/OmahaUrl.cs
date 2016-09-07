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
    /// The url tag section
    /// </summary>
    [XmlRoot("url")]
    public class OmahaUrl
    {
        /// <summary>
        /// The location of the download.
        /// </summary>
        [XmlAttribute("codebase")]
        public string Codebase { get; set; }
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        public OmahaUrl()
        {
            Codebase = "";
        }
    }
}
