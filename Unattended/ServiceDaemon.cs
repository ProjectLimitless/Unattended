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
using System.Configuration;
using Limitless.Unattended.Configuration;

namespace Limitless.Unattended
{
    /// <summary>
    /// The core application executed to launch the process to update.
    /// </summary>
    class ServiceDaemon
    {
        static void Main(string[] args)
        {
            UnattendedSection settings = (UnattendedSection)(dynamic)ConfigurationManager.GetSection("unattended");
            Unattended runner = new Unattended(settings);

            runner.Run();

            Console.WriteLine("Press <enter> to continue...");
            Console.ReadLine();            
        }
    }
}
