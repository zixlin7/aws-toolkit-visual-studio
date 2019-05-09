using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using BuildCommon;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BuildTasks.Test
{
    [TestClass]
    public class UploadHostedFileTaskTests
    {
        private static readonly ListObjectsResponse ListObjectsNoMatchResponse = new ListObjectsResponse()
        {
            IsTruncated = false,
            S3Objects = new List<S3Object>(),
        };

        private readonly List<string> _tempFilenames = new List<string>();
        private Mock<IBuildEngine4> _mockBuildEngine;
        private readonly List<string> _logMessages = new List<string>();

        [TestInitialize]
        public void TestSetup()
        {
            _mockBuildEngine = new Mock<IBuildEngine4>();

            Action<BuildEventArgs> captureEventMessage = buildMessageEventArgs =>
            {
                _logMessages.Add(buildMessageEventArgs.Message);
            };

            _mockBuildEngine.Setup(m => m.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildEventArgs>(captureEventMessage);
            _mockBuildEngine.Setup(m => m.LogMessageEvent(It.IsAny<BuildMessageEventArgs>())).Callback<BuildEventArgs>(captureEventMessage);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _tempFilenames.ForEach(File.Delete);
        }

        [TestMethod]
        public void NoFileExistsLocallyOrInS3()
        {
            var mockS3 = new Mock<IAmazonS3>();

            mockS3.Setup(m => m.ListObjects(It.IsAny<ListObjectsRequest>())).Returns(() => ListObjectsNoMatchResponse);

            var uploadTask = new UploadHostedFileTaskTestImpl(mockS3.Object)
            {
                BuildEngine = _mockBuildEngine.Object,
                Bucket = "bucket",
                S3Key = "nonExistingFile",
                LocalFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            };

            Assert.IsFalse(uploadTask.Execute());

            Assert.IsTrue(uploadTask.Log.HasLoggedErrors);

            mockS3.Verify(m => m.ListObjects(It.IsAny<ListObjectsRequest>()), Times.Once);
            mockS3.Verify(m => m.PutObject(It.IsAny<PutObjectRequest>()), Times.Never);
            Assert.IsTrue(LogMessagesContains(new List<string>()
            {
                "file is not in S3 bucket",
                "or locally:"
            }));
        }

        [TestMethod]
        public void FileExistsLocallyNotInS3()
        {
            var tempFilename = CreateTempFile("hello world");
            var mockS3 = new Mock<IAmazonS3>();

            mockS3.Setup(m => m.ListObjects(It.IsAny<ListObjectsRequest>())).Returns(() => ListObjectsNoMatchResponse);

            var uploadTask = new UploadHostedFileTaskTestImpl(mockS3.Object)
            {
                BuildEngine = _mockBuildEngine.Object,
                Bucket = "bucket",
                S3Key = "nonExistingFile",
                LocalFilename = tempFilename,
            };

            Assert.IsTrue(uploadTask.Execute());

            Assert.IsFalse(uploadTask.Log.HasLoggedErrors);

            mockS3.Verify(m => m.ListObjects(It.IsAny<ListObjectsRequest>()), Times.Once);
            mockS3.Verify(m => m.PutObject(It.IsAny<PutObjectRequest>()), Times.Once);
            Assert.IsTrue(LogMessagesContains("Pushing file"));
        }

        [TestMethod]
        public void FileExistsInS3NotLocally()
        {
            var mockS3 = new Mock<IAmazonS3>();

            mockS3.Setup(m => m.ListObjects(It.IsAny<ListObjectsRequest>())).Returns(() => new ListObjectsResponse()
            {
                IsTruncated = false,
                S3Objects = new List<S3Object>()
                {
                    new S3Object()
                    {
                        BucketName = "bucket",
                        ETag = "12345",
                        Key = "existingFile",
                    }
                }
            });

            var uploadTask = new UploadHostedFileTaskTestImpl(mockS3.Object)
            {
                BuildEngine = _mockBuildEngine.Object,
                Bucket = "bucket",
                S3Key = "existingFile",
                LocalFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            };

            Assert.IsFalse(uploadTask.Execute());

            Assert.IsTrue(uploadTask.Log.HasLoggedErrors);

            mockS3.Verify(m => m.ListObjects(It.IsAny<ListObjectsRequest>()), Times.Once);
            mockS3.Verify(m => m.PutObject(It.IsAny<PutObjectRequest>()), Times.Never);
            Assert.IsTrue(LogMessagesContains(new List<string>()
            {
                "Cancelling upload. The file exists in S3 bucket",
                "but not on local system:"
            }));
        }

        [TestMethod]
        public void SameFileLocallyAndInS3()
        {
            var tempFilename = CreateTempFile("hello world");
            var mockS3 = new Mock<IAmazonS3>();

            mockS3.Setup(m => m.ListObjects(It.IsAny<ListObjectsRequest>())).Returns(() => new ListObjectsResponse()
            {
                IsTruncated = false,
                S3Objects = new List<S3Object>()
                {
                    new S3Object()
                    {
                        BucketName = "bucket",
                        ETag = FileMD5Util.GenerateMD5Hash(tempFilename),
                        Key = "existingFile",
                    }
                }
            });

            var uploadTask = new UploadHostedFileTaskTestImpl(mockS3.Object)
            {
                BuildEngine = _mockBuildEngine.Object,
                Bucket = "bucket",
                S3Key = "existingFile",
                LocalFilename = tempFilename,
            };

            Assert.IsTrue(uploadTask.Execute());

            Assert.IsFalse(uploadTask.Log.HasLoggedErrors);

            mockS3.Verify(m => m.ListObjects(It.IsAny<ListObjectsRequest>()), Times.Once);
            mockS3.Verify(m => m.PutObject(It.IsAny<PutObjectRequest>()), Times.Never);
            Assert.IsTrue(LogMessagesContains("No differences found between S3 and local:"));
        }

        [TestMethod]
        public void DifferentFileLocallyAndInS3()
        {
            var tempFilenameLocal = CreateTempFile("hello world locally");
            var tempFilenameS3 = CreateTempFile("hello world from S3");
            var mockS3 = new Mock<IAmazonS3>();

            mockS3.Setup(m => m.ListObjects(It.IsAny<ListObjectsRequest>())).Returns(() => new ListObjectsResponse()
            {
                IsTruncated = false,
                S3Objects = new List<S3Object>()
                {
                    new S3Object()
                    {
                        BucketName = "bucket",
                        ETag = FileMD5Util.GenerateMD5Hash(tempFilenameS3),
                        Key = "existingFile",
                    }
                }
            });

            var uploadTask = new UploadHostedFileTaskTestImpl(mockS3.Object)
            {
                BuildEngine = _mockBuildEngine.Object,
                Bucket = "bucket",
                S3Key = "existingFile",
                LocalFilename = tempFilenameLocal,
            };

            Assert.IsTrue(uploadTask.Execute());

            Assert.IsFalse(uploadTask.Log.HasLoggedErrors);

            mockS3.Verify(m => m.ListObjects(It.IsAny<ListObjectsRequest>()), Times.Once);
            mockS3.Verify(m => m.PutObject(It.IsAny<PutObjectRequest>()), Times.Once);
            Assert.IsTrue(LogMessagesContains("Pushing file"));
        }

        /// <summary>
        /// Files created by this function will be automatically deleted at the end of the test
        /// </summary>
        /// <param name="contents">Contents to write to file</param>
        /// <returns>full path and filename of temp file</returns>
        private string CreateTempFile(string contents)
        {
            var filename = Path.GetTempFileName();

            using (var fileStream = File.OpenWrite(filename))
            using (var writer = new StreamWriter(fileStream))
            {
                writer.Write(contents);
            }

            _tempFilenames.Add(filename);
            return filename;
        }

        private bool LogMessagesContains(string text)
        {
            return LogMessagesContains(Enumerable.Repeat(text, 1));
        }

        /// <summary>
        /// Returns true if a message contains all texts in the given collection
        /// </summary>
        private bool LogMessagesContains(IEnumerable<string> texts)
        {
            return _logMessages.Any(message => texts.All(message.Contains));
        }
    }
}
