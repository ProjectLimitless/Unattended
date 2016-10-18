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
using System.Threading;
using System.Threading.Tasks;

namespace Limitless.Unattended
{
    /// <summary>
    /// Runs a task at specified intervals. Used from StackOverflow answer http://stackoverflow.com/a/23814733
    /// </summary>
    public class PeriodicTask
    {
        /// <summary>
        /// Async Runs action at the specified period intervals.
        /// </summary>
        /// <param name="action">The task to execute</param>
        /// <param name="period">The interval</param>
        /// <param name="cancellationToken">To stop the task</param>
        /// <returns>async Task</returns>
        public static async Task Run(Action action, TimeSpan period, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(period, cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                    action();
            }
        }

        /// <summary>
        /// Helper shortcut for Run.
        /// </summary>
        /// <param name="action">The task to execute</param>
        /// <param name="period">The interval</param>
        /// <returns>async Task</returns>
        public static Task Run(Action action, TimeSpan period)
        {
            return Run(action, period, CancellationToken.None);
        }
    }
}
