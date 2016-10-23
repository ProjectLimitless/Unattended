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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Limitless.ioRPC.Interfaces;

namespace SampleApplication
{
    /// <summary>
    /// Sample handler for the update.
    /// </summary>
    public class UpdateHandler : IRPCAsyncHandler
    {
        private Action<string, object> asyncCallback;

        public void CanUpdate()
        {
            Task.Run(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(20));
                asyncCallback("CanUpdate", true);
            });
        }

        public string Ping()
        {
            return "Pong Version 1.0.0.0";
        }

        public void Exit()
        {
            Environment.Exit(0);
        }

        public void SetAsyncCallback(Action<string, object> asyncCallback)
        {
            this.asyncCallback = asyncCallback;
        }
    }
}
