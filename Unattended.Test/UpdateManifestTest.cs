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

using NUnit.Framework;
using Limitless.Unattended.Structs;

namespace Unattended.Test
{
    [TestFixture]
    public class UpdateManifestTest
    {
        [Test]
        public void CanCreateFromFile()
        {
            UpdateManifest manifest = UpdateManifest.FromFile(@"..\Unattended.Test\TestFiles\TestManifest.uum");
            Assert.AreEqual("testapp", manifest.AppID);
            Assert.AreEqual("TestApp.exe", manifest.AppPath);
            Assert.AreEqual("http://unattendedserver.local", manifest.ServerUri);
        }
    }
}
