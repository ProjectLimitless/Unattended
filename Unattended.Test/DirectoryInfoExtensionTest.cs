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

using System.IO;
using NUnit.Framework;
using Limitless.Unattended.Extensions;

namespace Unattended.Test
{
    [TestFixture]
    public class DirectoryInfoExtensionTest
    {
        [Test]
        public void CanDeepCopy()
        {
            if (Directory.Exists(@".\Unattended.Test\TestFiles\DeepCopyTest\Destination"))
            {
                Directory.Delete(@".\Unattended.Test\TestFiles\DeepCopyTest\Destination", true);
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(@".\Unattended.Test\TestFiles\DeepCopyTest\Source");
            directoryInfo.DeepCopyTo(@".\Unattended.Test\TestFiles\DeepCopyTest\Destination");

            // Verify file is there
            string[] directories = Directory.GetDirectories(@".\Unattended.Test\TestFiles\DeepCopyTest\Destination");
            Assert.Contains(@".\Unattended.Test\TestFiles\DeepCopyTest\Destination\Sub", directories);
            string[] files = Directory.GetFiles(@".\Unattended.Test\TestFiles\DeepCopyTest\Destination\Sub");
            Assert.Contains(@".\Unattended.Test\TestFiles\DeepCopyTest\Destination\Sub\File.txt", files);
        }
    }
}
