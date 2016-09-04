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
using System.Xml.Serialization;

namespace Limitless.Unattended.Structs
{
    /// <summary>
    /// The app tag section
    /// </summary>
    [XmlRoot("app")]
    public class OmahaApp
    {
        /// <summary>
        /// The unique application ID.
        /// </summary>
        [XmlAttribute("appid")]
        public string ID { get; set; }
        /// <summary>
        /// The symver version of the application.
        /// </summary>
        [XmlAttribute("version")]
        public string Version { get; set; }
        /// <summary>
        /// The update channel, defaults to 'stable'.
        /// </summary>
        [XmlAttribute("track")]
        public string Channel { get; set; }
        /// <summary>
        /// The unique client ID for this instance.
        /// </summary>
        [XmlAttribute("bootid")]
        public string ClientID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [XmlElement("event")]
        public OmahaEvent Event { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public OmahaApp()
        {
            Channel = "stable";
            Event = new OmahaEvent();
        }       
    }
}
