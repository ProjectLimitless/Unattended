﻿/** 
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
    /// Contains the possible update strategies.
    /// </summary>
    public static class UpdateStrategy
    {
        /// <summary>
        /// In this strategy, Unattended will query the child application via ioRPC
        /// if it is ready for a restart. Unattended will only restart the application
        /// when an 'Ok' is received, regardless of period it had to wait for the response.
        /// </summary>
        public const string Prompt = "prompt";
        /// <summary>
        /// In the 'restart' strategy, the child application will be restarted as soon
        /// as an update has been completely applied.
        /// </summary>
        public const string Restart = "restart";
        /// <summary>
        /// The default update strategy. Set to 'Restart'.
        /// </summary>
        public const string Default = Restart;

        /// <summary>
        /// Checks if the given strategy is valid.
        /// </summary>
        /// <param name="strategy">The strategy as a string</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValid(string strategy)
        {
            switch (strategy)
            {
                case UpdateStrategy.Prompt:
                    return true;
                case UpdateStrategy.Restart:
                    return true;
                default:
                    return false;
            }
        }
    }
}
