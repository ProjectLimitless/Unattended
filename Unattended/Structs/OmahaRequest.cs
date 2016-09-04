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
    /// The subset implementation of the Omaha request protocol.
    /// </summary>
    [XmlRoot("request")]
    public class OmahaRequest
    {
        /// <summary>
        /// Omaha protocol version.
        /// </summary>
        [XmlAttribute("protocol")]
        public decimal Protocol { get; set; }
        /// <summary>
        /// The Application section.
        /// </summary>
        [XmlElement("app")]
        public OmahaApp Application { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public OmahaRequest()
        {
            Protocol = 3.0M;
            Application = new OmahaApp();
        }

        /// <summary>
        /// Constructor with the application section defined.
        /// </summary>
        /// <param name="application">The Omaha application section</param>
        public OmahaRequest(OmahaApp application)
        {
            Protocol = 3.0M;
            Application = application;
        }
    }
}
