using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;

using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Jobs;

namespace Amazon.AWSToolkit.Tests.S3
{
    /// <summary>
    /// Summary description for DownloadFilesTests
    /// </summary>
    [TestClass]
    public class DownloadFilesTests
    {
        public DownloadFilesTests()
        {
            //
            // TODO: Add constructor logic here
            //
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
        public void DownloadTest()
        {
            string bucketName = "dotnet-integ-DownloadTest" + DateTime.Now.Ticks;
            Clients.S3Client.PutBucket(new PutBucketRequest() { BucketName = bucketName });
            try
            {
                Clients.S3Client.PutObject(new PutObjectRequest()
                {
                    BucketName = bucketName,
                    Key = "file1.txt",
                    ContentBody = "some stuff"
                });
                Clients.S3Client.PutObject(new PutObjectRequest()
                {
                    BucketName = bucketName,
                    Key = "sub/file2.txt",
                    ContentBody = "some stuff"
                });

                Clients.S3Client.PutObject(new PutObjectRequest()
                {
                    BucketName = bucketName,
                    Key = "sub/sub2/file3.txt",
                    ContentBody = "some stuff"
                });


                BucketBrowserModel model = new BucketBrowserModel(bucketName);
                model.ChildItems.Add(new BucketBrowserModel.ChildItem("file1.txt", BucketBrowserModel.ChildType.File));
                model.ChildItems.Add(new BucketBrowserModel.ChildItem("sub", BucketBrowserModel.ChildType.Folder));

                BucketBrowserController command = new BucketBrowserController(Clients.S3Client, model);
                DownloadFilesJob downloadFiles = new DownloadFilesJob(command, bucketName, "/", model.ChildItems.DisplayedChildItems.ToArray(), @"c:\temp\lower\level\");
                downloadFiles.Execute();
            }
            finally
            {
                IOTests.DeleteBucket(bucketName);
            }
        }
    }
}
