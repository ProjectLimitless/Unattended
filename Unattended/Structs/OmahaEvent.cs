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
    /// The event tag section
    /// </summary>
    [XmlRoot("event")]
    public class OmahaEvent
    {
        /// <summary>
        /// The Omaha event type.
        /// Defaults to 'Unknown (0)'
        /// </summary>
        [XmlAttribute("eventtype")]
        public OmahaEventType EventType { get; set; }
        /// <summary>
        /// The Omaha event result.
        /// Defaults to 'No Update (7)'
        /// </summary>
        [XmlAttribute("eventresult")]
        public OmahaEventResultType EventResult { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public OmahaEvent()
        {
            EventType = OmahaEventType.Unknown;
            EventResult = OmahaEventResultType.NoUpdate;
        }

        /// <summary>
        /// Constructor with event type and result.
        /// </summary>
        /// <param name="eventType">The event type to set the event to</param>
        /// <param name="eventResult">The event result to set the event to</param>
        public OmahaEvent(OmahaEventType eventType, OmahaEventResultType eventResult)
        {
            EventType = eventType;
            EventResult = eventResult;
        }
    }
}
