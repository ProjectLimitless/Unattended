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
using System.Configuration;
using Limitless.Unattended;
using Limitless.Unattended.Configuration;

namespace Unattended.Test
{
    [TestFixture]
    public class UnattendedTest
    {
        UnattendedSection settings;

        [SetUp]
        public void Setup()
        {
            // It's important to test that we can read the custom section of the app config
            // a couple of tests will ensure that
            settings = (UnattendedSection)(dynamic)ConfigurationManager.GetSection("unattended");
        }
        
        [Test]
        public void MustParseCustomConfigSection()
        {
            Assert.IsNotNull(settings);
        }

        [Test]
        public void MustParseClientID()
        {
           Assert.AreEqual("demo", settings.ClientID);
        }

        [Test]
        public void MustParseConfigurationDirectory()
        {
           Assert.AreEqual(@".\Unattended.Test\TestFiles", settings.ConfigurationDirectory);
        }

        [Test]
        public void MustParseUpdateStrategy()
        {
            Assert.AreEqual(UpdateStrategy.Prompt, settings.Updates.Strategy);
        }

        [Test]
        public void MustParseUpdateInterval()
        {
            Assert.AreEqual(UpdateInterval.Daily, settings.Updates.Interval);
        }

        [Test]
        public void MustParseTargetBase()
        {
            Assert.AreEqual(@".\Unattended.Test\TestFiles", settings.Target.BasePath);
        }

        [Test]
        public void MustParseTargetFilename()
        {
            Assert.AreEqual("SampleFile.txt", settings.Target.Filename);
        }

        [Test]
        public void MustParseTargetParameters()
        {
            Assert.AreEqual("-test true", settings.Target.Parameters);
        }

        [Test]
        public void MustParseUpdateChannel()
        {
            Assert.AreEqual("stable", settings.Updates.Channel);
        }

        [Test]
        public void IsValidIntervals()
        {
            Assert.IsTrue(UpdateInterval.IsValid("daily"));
            Assert.IsTrue(UpdateInterval.IsValid("hourly"));
        }

        [Test]
        public void IsValidStrategies()
        {
            Assert.IsTrue(UpdateStrategy.IsValid("prompt"));
            Assert.IsTrue(UpdateStrategy.IsValid("restart"));
        }

        [Test]
        public void CreateNewUnattended()
        {
            Limitless.Unattended.Unattended runner = new Limitless.Unattended.Unattended(settings);
        }
    }
}
