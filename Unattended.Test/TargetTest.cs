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
using System.IO;
using System.Collections.Generic;

using NUnit.Framework;
using Limitless.Unattended.Structs;
using Limitless.Unattended.Configuration;

namespace Unattended.Test
{
    [TestFixture]
    public class TargetTest
    {
        TargetSection section;
        Target target;

        [SetUp]
        public void Setup()
        {
            section = new TargetSection();
            section.BasePath = @"..\Unattended.Test\TestFiles";
            section.Filename = "SampleFile.txt";
            section.Parameters = "-test true";
            target = new Target(section);
        }

        [Test]
        public void SetDefaultPathFormat()
        {
            Target target = new Target(section);
            Assert.IsNotNull(target.VersionPathFormat);
            Assert.IsNotEmpty(target.VersionPathFormat);
        }

        [Test]
        public void SetCustomPathFormat()
        {
            Target testTarget = new Target(section, "yyyyMMdd");
            Assert.AreEqual("yyyyMMdd", testTarget.VersionPathFormat);
        }

        [Test]
        public void GetApplicationParameters()
        {
            Assert.AreEqual("-test true", target.ApplicationParameters);
        }

        [Test]
        public void FailBasePathCheck()
        {
            Assert.Throws(typeof(IOException), new TestDelegate(InvalidBasePathException));
        }

        [Test]
        public void FailApplicationPathCheck()
        {
            Assert.Throws(typeof(IOException), new TestDelegate(InvalidTargetException));
        }

        [Test]
        public void VerifyCurrentVersion()
        {
            KeyValuePair<DateTime, int>? version = target.CurrentVersion();
            Assert.IsNotNull(version);
            Assert.AreEqual(DateTime.Parse("2016-02-01"), version.Value.Key);
            Assert.AreEqual(1, version.Value.Value);
        }

        [Test]
        public void VerifyCurrentVersionDirectory()
        {
            string currentVersionDirectory = target.CurrentVersionDirectory();
            StringAssert.Contains(@"Unattended.Test\TestFiles\20160201.1", currentVersionDirectory);   
        }

        [Test]
        public void VerifyLatestVersion()
        {
            KeyValuePair<DateTime, int>? version = target.LatestVersion();
            Assert.IsNotNull(version);
            Assert.AreEqual(DateTime.Parse("2016-02-01"), version.Value.Key);
            Assert.AreEqual(1, version.Value.Value);
        }

        [Test]
        public void VerifyLatestVersionDirectory()
        {
            string latestVersionDirectory = target.CurrentVersionDirectory();
            StringAssert.Contains(@"Unattended.Test\TestFiles\20160201.1", latestVersionDirectory);
        }

        [Test]
        public void VerifyPreviousVersionDirectory()
        {
            string previousVersionDirectory = target.PreviousVersionDirectory();
            StringAssert.Contains(@"Unattended.Test\TestFiles\20160129.5", previousVersionDirectory);
        }
        
        [Test]
        public void MustUpdateTargetPath()
        {
            TargetSection testSection = section;
            Target testTarget = target;
            // Create a new 'updated' directory to ensure update picks the new one
            Directory.CreateDirectory(@"..\Unattended.Test\TestFiles\21000102.1");
            FileStream fs = File.Create(@"..\Unattended.Test\TestFiles\21000102.1\SampleFile.txt");
            fs.Flush();
            fs.Close();
            testTarget.Update();

            // Verify the new current verion
            KeyValuePair<DateTime, int>? version = testTarget.CurrentVersion();
            Assert.IsNotNull(version);
            Assert.AreEqual(DateTime.Parse("2100-01-02"), version.Value.Key);
            Assert.AreEqual(1, version.Value.Value);

            testTarget.Rollback();
            version = testTarget.CurrentVersion();
            Assert.IsNotNull(version);
            Assert.AreEqual(DateTime.Parse("2016-02-01"), version.Value.Key);
            Assert.AreEqual(1, version.Value.Value);


            if (Directory.Exists(@"..\Unattended.Test\TestFiles\21000102.1"))
            {
                Directory.Delete(@"..\Unattended.Test\TestFiles\21000102.1", true);
            }
        }

        void InvalidBasePathException()
        {
            string basePath = section.BasePath;
            section.BasePath = "nonexizst";
            new Target(section);
            section.BasePath = basePath;
        }

        void InvalidTargetException()
        {
            string filename = section.Filename;
            section.Filename = "nonexizst";
            new Target(section);
            section.Filename = filename;
        }

    }
}
