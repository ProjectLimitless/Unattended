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
    public enum OmahaEventType
    {
        [XmlEnum("0")]
        Unknown,
        /// <summary>
        /// The event type to submit to check for update.
        /// </summary>
        [XmlEnum("1")]
        UpdateCheck,
        /// <summary>
        /// Event type for results regarding downloads.
        /// </summary>
        [XmlEnum("2")]
        UpdateDownload,
        /// <summary>
        /// Event type for results regarding installation.
        /// </summary>
        [XmlEnum("3")]
        UpdateInstall,
        /// <summary>
        /// Event type for results regarding rollback.
        /// </summary>
        [XmlEnum("4")]
        UpdateRollback,
        /// <summary>
        /// Event type for ping tests.
        /// </summary>
        [XmlEnum("800")]
        Ping
    }
}
