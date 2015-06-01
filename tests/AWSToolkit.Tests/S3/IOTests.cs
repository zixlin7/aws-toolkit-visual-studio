using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;

namespace Amazon.AWSToolkit.Tests.S3
{
    /// <summary>
    /// Summary description for IOTests
    /// </summary>
    [TestClass]
    public class IOTests
    {

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
        public void GetDirectoriesTest()
        {
            string bucketName = "dotnet-integ-getdirectories" + DateTime.Now.Ticks;
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

                string[] directories = S3Directory.GetDirectories(Clients.S3Client, bucketName, "/");
                Assert.AreEqual(1, directories.Length);
                Assert.AreEqual("sub", directories[0]);

                string[] files = S3Directory.GetFiles(Clients.S3Client, bucketName, "/");
                Assert.AreEqual(1, files.Length);
                Assert.AreEqual("file1.txt", files[0]);


                directories = S3Directory.GetDirectories(Clients.S3Client, bucketName, "/sub");
                Assert.AreEqual(1, directories.Length);
                Assert.AreEqual("sub/sub2", directories[0]);

                files = S3Directory.GetFiles(Clients.S3Client, bucketName, "/sub");
                Assert.AreEqual(1, files.Length);
                Assert.AreEqual("sub/file2.txt", files[0]);

                directories = S3Directory.GetDirectories(Clients.S3Client, bucketName, "/sub/sub2");
                Assert.AreEqual(0, directories.Length);

                files = S3Directory.GetFiles(Clients.S3Client, bucketName, "/sub/sub2");
                Assert.AreEqual(1, files.Length);
                Assert.AreEqual("sub/sub2/file3.txt", files[0]);

            }
            finally
            {
                DeleteBucket(bucketName);
            }
        }

        [TestMethod]
        public void GetDirectoriesAndFilesTest()
        {
            string bucketName = "dotnet-integ-GetDirectoriesAndFiles" + DateTime.Now.Ticks;
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

                string[] directories, files;

                S3Directory.GetDirectoriesAndFiles(Clients.S3Client, bucketName, "/", out directories, out files);
                Assert.AreEqual(1, directories.Length);
                Assert.AreEqual("sub", directories[0]);

                Assert.AreEqual(1, files.Length);
                Assert.AreEqual("file1.txt", files[0]);


                S3Directory.GetDirectoriesAndFiles(Clients.S3Client, bucketName, "/sub", out directories, out files);
                Assert.AreEqual(1, directories.Length);
                Assert.AreEqual("sub/sub2", directories[0]);

                Assert.AreEqual(1, files.Length);
                Assert.AreEqual("sub/file2.txt", files[0]);

                S3Directory.GetDirectoriesAndFiles(Clients.S3Client, bucketName, "/sub/sub2", out directories, out files);
                Assert.AreEqual(0, directories.Length);

                Assert.AreEqual(1, files.Length);
                Assert.AreEqual("sub/sub2/file3.txt", files[0]);

            }
            finally
            {
                DeleteBucket(bucketName);
            }
        }

        [TestMethod]
        public void GetFilesRecursively()
        {
            string bucketName = "dotnet-integ-GetFilesRecursively" + DateTime.Now.Ticks;
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


                string[] files = S3Directory.GetFiles(Clients.S3Client, bucketName, "/", System.IO.SearchOption.AllDirectories);
                Assert.AreEqual(3, files.Length);

                files = S3Directory.GetFiles(Clients.S3Client, bucketName, "/sub/", System.IO.SearchOption.AllDirectories);
                Assert.AreEqual(2, files.Length);

                files = S3Directory.GetFiles(Clients.S3Client, bucketName, "/sub", System.IO.SearchOption.AllDirectories);
                Assert.AreEqual(2, files.Length);

                files = S3Directory.GetFiles(Clients.S3Client, bucketName, "sub", System.IO.SearchOption.AllDirectories);
                Assert.AreEqual(2, files.Length);

                files = S3Directory.GetFiles(Clients.S3Client, bucketName, "/sub/sub2", System.IO.SearchOption.AllDirectories);
                Assert.AreEqual(1, files.Length);

                files = S3Directory.GetFiles(Clients.S3Client, bucketName, "/sub/sub2", System.IO.SearchOption.AllDirectories);
                Assert.AreEqual(1, files.Length);

                files = S3Directory.GetFiles(Clients.S3Client, bucketName, "sub/sub2", System.IO.SearchOption.AllDirectories);
                Assert.AreEqual(1, files.Length);
            }
            finally
            {
                DeleteBucket(bucketName);
            }
        }

        [TestMethod]
        public void CreateFolderTest()
        {
            string bucketName = "dotnet-integ-GetFilesRecursively" + DateTime.Now.Ticks;
            Clients.S3Client.PutBucket(new PutBucketRequest() { BucketName = bucketName });
            try
            {
                string[] directories;

                S3Directory.CreateDirectory(Clients.S3Client, bucketName, "home");
                directories = S3Directory.GetDirectories(Clients.S3Client, bucketName, "");
                Assert.AreEqual(1, directories.Length);
                Assert.AreEqual("home", directories[0]);

                S3Directory.CreateDirectory(Clients.S3Client, bucketName, "home/user");
                directories = S3Directory.GetDirectories(Clients.S3Client, bucketName, "home");
                Assert.AreEqual(1, directories.Length);
                Assert.AreEqual("home/user", directories[0]);

                S3Directory.CreateDirectory(Clients.S3Client, bucketName, "home/user/a");
                directories = S3Directory.GetDirectories(Clients.S3Client, bucketName, "home/user");
                Assert.AreEqual(1, directories.Length);
                Assert.AreEqual("home/user/a", directories[0]);

                S3Directory.CreateDirectory(Clients.S3Client, bucketName, "home/user/a/b");
                directories = S3Directory.GetDirectories(Clients.S3Client, bucketName, "home/user/a");
                Assert.AreEqual(1, directories.Length);
                Assert.AreEqual("home/user/a/b", directories[0]);

            }
            finally
            {
                DeleteBucket(bucketName);
            }

        }

        internal static void DeleteBucket(string bucketName)
        {
            Amazon.S3.Util.AmazonS3Util.DeleteS3BucketWithObjects(Clients.S3Client, bucketName);
        }
    }
}
