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

namespace SampleApplication
{
    /// <summary>
    /// Sample handler for the update.
    /// </summary>
    public class UpdateHandler
    {
        public bool CanUpdate()
        {
            Thread.Sleep(10000);
            return true;
        }

        public string Ping()
        {
            return "Pong A";
        }

        public void Exit()
        {
            Environment.Exit(0);
        }
    }
}
