using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.AWSToolkit.VersionInfo;

namespace Amazon.AWSToolkit.Tests
{
    [TestClass]
    public class ParseVersionFileTests
    {
        static string SAMPLE_CONTENT =
            "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
            "<versions location=\"https://w.amazon.com/\">"+
            "  <version number=\"0.1.0.5\" release-date=\"2010-01-19\">"+
            "    <change>Cool Stuff</change>"+
            "    <change>Even Cooler Stuff</change>"+
            "  </version>"+
            "  <version number=\"0.1.0.4\" release-date=\"2010-01-01\">"+
            "    <change>Cloud Front Stuff</change>"+
            "    <change>EC2 Stuff</change>"+
            "  </version>"+
            "</versions>";

        [TestMethod]
        public void ParseFile()
        {
            IList<VersionManager.Version> versions;
            string updateLocation;
            VersionManager.ParseVersionInfoFile(SAMPLE_CONTENT, out versions, out updateLocation);

            Assert.AreEqual(2, versions.Count);

            Assert.AreEqual("https://w.amazon.com/", updateLocation);

            Assert.AreEqual("0.1.0.5", versions[0].Number);
            Assert.AreEqual(2, versions[0].Changes.Count);
            Assert.AreEqual("Cool Stuff", versions[0].Changes[0]);
            Assert.AreEqual("Even Cooler Stuff", versions[0].Changes[1]);


            Assert.AreEqual("0.1.0.4", versions[1].Number);
            Assert.AreEqual(2, versions[1].Changes.Count);
            Assert.AreEqual("Cloud Front Stuff", versions[1].Changes[0]);
            Assert.AreEqual("EC2 Stuff", versions[1].Changes[1]);

        }
    }
}
