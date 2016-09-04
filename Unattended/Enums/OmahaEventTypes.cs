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

namespace Limitless.Unattended.Enums
{
    /// <summary>
    /// Custom event types for the (subset of) Omaha protocol.
    /// </summary>
    [Serializable]
    public enum OmahaEventTypes
    {
        [XmlEnum("0")]
        Unknown = 0,
        /// <summary>
        /// The event type to submit to check for update.
        /// </summary>
        [XmlEnum("1")]
        UpdateCheck = 1,
        /// <summary>
        /// Event type for results regarding downloads.
        /// </summary>
        [XmlEnum("2")]
        UpdateDownload = 2,
        /// <summary>
        /// Event type for results regarding installation.
        /// </summary>
        [XmlEnum("3")]
        UpdateInstall = 3,
        /// <summary>
        /// Event type for results regarding rollback.
        /// </summary>
        [XmlEnum("4")]
        UpdateRollback = 4,
        /// <summary>
        /// Event type for ping tests.
        /// </summary>
        [XmlEnum("800")]
        Ping = 800
    }
}
