using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.S3.Nodes;

namespace Amazon.AWSToolkit.Tests
{
    /// <summary>
    /// Summary description for EndPointTests
    /// </summary>
    [TestClass]
    public class EndPointTests
    {
        public EndPointTests()
        {
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void CheckS3EndPoint()
        {
            Assert.IsTrue(RegionEndPointsManager.Instance.Regions.Count() > 0);
            var endpoint = RegionEndPointsManager.Instance.GetRegion("us-east-1").GetEndpoint(RegionEndPointsManager.S3_SERVICE_NAME);
            Assert.AreEqual("us-east-1", endpoint.RegionSystemName);
            Assert.AreEqual("https://s3.amazonaws.com/", endpoint.Url);
        }

        [TestMethod]
        public void TestS3FileFetcher()
        {
            string content = S3FileFetcher.Instance.GetFileContent(Constants.VERSION_INFO_FILE, S3FileFetcher.CacheMode.Never);
            Assert.IsNotNull(content);
        }

        [TestMethod]
        public void TestS3FileFetcherWithLocalFile()
        {
            S3FileFetcher.Instance.HostedFilesLocation = new Uri("file://c:/Windows/");

            string content = S3FileFetcher.Instance.GetFileContent("system.ini");
            Assert.IsNotNull(content);

            S3FileFetcher.Instance.HostedFilesLocation = new Uri("file://c:/Windows"); // Without a slash

            content = S3FileFetcher.Instance.GetFileContent("system.ini");
            Assert.IsNotNull(content);
        }

        [TestMethod]
        public void TestS3FetcherWithURLOverride()
        {
            S3FileFetcher.Instance.HostedFilesLocation = new Uri("http://aws.amazon.com/");
            string content = S3FileFetcher.Instance.GetFileContent("index.html");
            Assert.IsNotNull(content);
        }

        [TestMethod]
        public void TestS3FetcherWithRegionScheme()
        {
            S3FileFetcher.Instance.HostedFilesLocation = new Uri("region://cn-north-1");
            Uri endpoint = S3FileFetcher.Instance.HostedFilesLocation;
            Assert.AreEqual(endpoint.AbsoluteUri, "https://aws-vs-toolkit-cn-north-1.s3.cn-north-1.amazonaws.com.cn/");
        }
    }
}
