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
    public enum OmahaEventResultType
    {
        [XmlEnum("0")]
        Unknown,
        /// <summary>
        /// The result when no update is available.
        /// </summary>
        [XmlEnum("1")]
        NoUpdate,
        /// <summary>
        /// The result when a new update is available.
        /// </summary>
        [XmlEnum("2")]
        Available,
        /// <summary>
        /// Operation success.
        /// </summary>
        [XmlEnum("3")]
        Success,
        /// <summary>
        /// Success and app restarted.
        /// </summary>
        [XmlEnum("4")]
        SuccessRestarted,
        /// <summary>
        /// Operation failed.
        /// </summary>
        [XmlEnum("5")]
        Error,
        /// <summary>
        /// Operation cancelled.
        /// </summary>
        [XmlEnum("6")]
        Cancelled,
        /// <summary>
        /// Operation started.
        /// </summary>
        [XmlEnum("7")]
        Started
    }
}
