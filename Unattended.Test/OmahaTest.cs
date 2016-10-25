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
using Limitless.Unattended.Enums;
using System.Xml.Serialization;
using System.IO;
using System;

namespace Unattended.Test
{
    [TestFixture]
    public class OmahaTest
    {
        [Test]
        public void CanSerializeRequest()
        {
            OmahaRequest request = new OmahaRequest();
            request.Application.ClientID = "demo";
            request.Application.ID = "sampleapp";
            request.Application.Version = "1.0.0.0";
            request.Application.Event.EventType = OmahaEventType.UpdateCheck;
            request.Application.Event.EventResult = OmahaEventResultType.Started;

            string expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<request xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" protocol=""3.0"">
  <app appid=""sampleapp"" version=""1.0.0.0"" track=""stable"" bootid=""demo"">
    <event eventtype=""1"" eventresult=""7"" />
  </app>
</request>";

            XmlSerializer serializer = new XmlSerializer(request.GetType());
            using (StringWriter textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, request);
                Assert.AreEqual(expected, textWriter.ToString());
            }
        }

        [Test]
        public void CanDeserializeNoUpdateResponse()
        {
            string responseString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><response protocol=\"3.0\" server=\"unattendedserver.local\"><app appid=\"sampleapp\" status=\"ok\"><updatecheck status=\"noupdate\"></updatecheck></app></response>";
            XmlSerializer parser = new XmlSerializer(typeof(OmahaResponse));
            OmahaResponse omahaResponse = (OmahaResponse)parser.Deserialize(new StringReader(responseString));

            Assert.AreEqual("ok", omahaResponse.Application.Status);
            Assert.AreEqual("noupdate", omahaResponse.Application.UpdateCheck.Status);
        }

        [Test]
        public void CanDeserializeAvailableUpdateResponse()
        {
            string responseString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><response protocol=\"3.0\" server=\"unattendedserver.local\"><app appid=\"sampleapp\" status=\"ok\"><updatecheck status=\"ok\"><manifest version=\"1.0.0.1\" trace=\"demo-zZZuNwGqbxyrAc5p3itWadIIGSKB10pq\"><url codebase=\"http://unattendedserver.local/packages/sampleapp.v1.0.0.1.zip\"></url><package hash=\"4fc21f834edfe576babd0f9e9eb0fe2327af9c256d7209550c9757c5e7efc47a\" name=\"sampleapp.v1.0.0.1.zip\" size=\"2522\"></package></manifest></updatecheck></app></response>";
            XmlSerializer parser = new XmlSerializer(typeof(OmahaResponse));
            OmahaResponse omahaResponse = (OmahaResponse)parser.Deserialize(new StringReader(responseString));

            Assert.AreEqual("ok", omahaResponse.Application.UpdateCheck.Status);
            OmahaManifest omahaManifest = omahaResponse.Application.UpdateCheck.Manifest;
            Assert.AreEqual(3.0, omahaResponse.Protocol);
            Assert.NotNull(omahaManifest);
            Assert.AreEqual("4fc21f834edfe576babd0f9e9eb0fe2327af9c256d7209550c9757c5e7efc47a", omahaManifest.Package.SHA256Hash);
            Assert.AreEqual("sampleapp.v1.0.0.1.zip", omahaManifest.Package.Name);
            Assert.AreEqual(2522, omahaManifest.Package.SizeInBytes);
            Assert.AreEqual("1.0.0.1", omahaManifest.Version);
            Assert.AreEqual("http://unattendedserver.local/packages/sampleapp.v1.0.0.1.zip", omahaManifest.Url.Codebase);

        }
    }
}
