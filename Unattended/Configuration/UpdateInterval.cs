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

namespace Limitless.Unattended.Configuration
{
    /// <summary>
    /// Checking intervals for update.
    /// </summary>
    public static class UpdateInterval
    {
        /// <summary>
        /// Check for updates once a day.
        /// </summary>
        public const string Daily = "daily";
        /// <summary>
        /// Check for updates once an hour.
        /// </summary>
        public const string Hourly = "hourly";
        /// <summary>
        /// The default update interval. Set to 'Hourly'.
        /// </summary>
        public const string Default = Hourly;

        /// <summary>
        /// Checks if the given interval is valid.
        /// </summary>
        /// <param name="interval">The interval as a string</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValid(string interval)
        {
            switch (interval)
            {
                case UpdateInterval.Daily:
                    return true;
                case UpdateInterval.Hourly:
                    return true;
                default:
                    return false;
            }
        }
    }
}
