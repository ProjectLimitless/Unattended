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
    /// Custom event result types for the (subset of) Omaha protocol.
    /// </summary>
    [Serializable]
    public enum OmahaEventResultTypes
    {
        [XmlEnum("0")]
        Unknown = 0,
        /// <summary>
        /// The result when no update is available.
        /// </summary>
        [XmlEnum("1")]
        NoUpdate = 1,
        /// <summary>
        /// The result when a new update is available.
        /// </summary>
        [XmlEnum("2")]
        Available = 2,
        /// <summary>
        /// Operation success.
        /// </summary>
        [XmlEnum("3")]
        Success = 3,
        /// <summary>
        /// Success and app restarted.
        /// </summary>
        [XmlEnum("4")]
        SuccessRestarted = 4,
        /// <summary>
        /// Operation failed.
        /// </summary>
        [XmlEnum("5")]
        Error = 5,
        /// <summary>
        /// Operation cancelled.
        /// </summary>
        [XmlEnum("6")]
        Cancelled = 6,
        /// <summary>
        /// Operation started.
        /// </summary>
        [XmlEnum("7")]
        Started = 7
    }
}
