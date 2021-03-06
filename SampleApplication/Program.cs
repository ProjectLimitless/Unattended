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
using Limitless.ioRPC;

namespace SampleApplication
{
    /// <summary>
    /// A simple sample program for showing how the Unattended updater works.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            UpdateHandler handler = new UpdateHandler();
            Client client = new Client(handler);
            client.Listen();
        }
    }
}
