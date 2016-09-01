

using Limitless.Unattended.Configuration;
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

namespace Limitless.Unattended
{
    /// <summary>
    /// The core application executed to launch the process to update.
    /// </summary>
    class ServiceDaemon
    {
        static void Main(string[] args)
        {
            Unattended runner = new Unattended();
            
            Console.WriteLine("Press <enter> to continue...");
            Console.ReadLine();            
        }
    }
}
