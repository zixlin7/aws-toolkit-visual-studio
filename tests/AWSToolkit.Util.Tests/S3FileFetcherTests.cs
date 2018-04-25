using System;
using System.IO;
using System.Net;
using Amazon.AWSToolkit;
using Xunit;
using Moq;

namespace AWSToolkit.Util.Tests
{
    public class S3FileFetcherTests
    {
        private const string DummyContentFromUserFolderLocation = "Dummy content found in user-configured location";
        private const string DummyContentFromCacheLocation = "Dummy content found in cache location";
        private const string EndpointsFilename = "ServiceEndPoints.xml"; // use real name so we detect accidental cache hits from probing
        private const string ContentFolder = "HostedFileTesting";

        /// <summary>
        /// Tests that if a user-configured location is set for hosted files, the endpoints 
        /// content is located there and returned.
        /// </summary>
        [Fact]
        public void TestLoadFromConfiguredFolder()
        {
            var tempLocation = Path.Combine(Path.GetTempPath(), ContentFolder);
            Directory.CreateDirectory(tempLocation);
            var dummyEndpointsPath = Path.Combine(tempLocation, EndpointsFilename);
            File.WriteAllText(dummyEndpointsPath, DummyContentFromUserFolderLocation);

            var mock = new Mock<IS3FileFetcherContentResolver>();
            mock.Setup(fetcher => fetcher.GetUserConfiguredLocalHostedFilesPath()).Returns(tempLocation);

            try
            {
                var s3FileFetcher = new S3FileFetcher(mock.Object);
                var content = string.Empty;
                using (var s = s3FileFetcher.OpenFileStream(EndpointsFilename, S3FileFetcher.CacheMode.Never))
                {
                    using (var reader = new StreamReader(s))
                    {
                        content = reader.ReadToEnd();
                    }
                }

                Assert.Equal(DummyContentFromUserFolderLocation, content);
                Assert.Equal(S3FileFetcher.ResolvedLocation.ConfiguredFolder, s3FileFetcher.ResolvedContentLocation);
            }
            finally
            {
                if (Directory.Exists(tempLocation))
                {
                    Directory.Delete(tempLocation, true);
                }
            }
        }

        /// <summary>
        /// Tests that if no user-configured location is set for hosted files, but data has been cached
        /// previously that the cached content is located and returned.
        /// </summary>
        [Fact]
        public void TestLoadFromUserProfileCache()
        {
            var tempLocation = Path.Combine(Path.GetTempPath(), ContentFolder);
            Directory.CreateDirectory(tempLocation);
            var dummyEndpointsPath = Path.Combine(tempLocation, EndpointsFilename);
            File.WriteAllText(dummyEndpointsPath, DummyContentFromCacheLocation);

            var mock = new Mock<IS3FileFetcherContentResolver>();
            mock.Setup(fetcher => fetcher.GetUserConfiguredLocalHostedFilesPath()).Returns((string)null);
            mock.Setup(fetcher => fetcher.GetLocalCachePath(EndpointsFilename)).Returns(Path.Combine(tempLocation, EndpointsFilename));

            try
            {
                var s3FileFetcher = new S3FileFetcher(mock.Object);
                var content = string.Empty;
                // use permanent mode, cache isn't hit for 'never'
                using (var s = s3FileFetcher.OpenFileStream(EndpointsFilename, S3FileFetcher.CacheMode.Permanent))
                {
                    using (var reader = new StreamReader(s))
                    {
                        content = reader.ReadToEnd();
                    }
                }

                Assert.Equal(DummyContentFromCacheLocation, content);
                Assert.Equal(S3FileFetcher.ResolvedLocation.Cache, s3FileFetcher.ResolvedContentLocation);
            }
            finally
            {
                if (Directory.Exists(tempLocation))
                {
                    Directory.Delete(tempLocation, true);
                }
            }
        }

        /// <summary>
        /// Tests that if no user-configured location is set for hosted files and no previously cached data
        /// is available, and we can't reach the CloudFront or S3 locations, that data from the application's
        /// resources is returned.
        /// </summary>
        [Fact]
        public void TestLoadFromFallbackResources()
        {
            var mock = new Mock<IS3FileFetcherContentResolver>();
            mock.Setup(fetcher => fetcher.GetUserConfiguredLocalHostedFilesPath()).Returns((string)null);
            mock.Setup(fetcher => fetcher.GetLocalCachePath(EndpointsFilename)).Returns((string)null);
            mock.Setup(fetcher => fetcher.HostedFilesLocation).Returns((Uri)null);
            // this will trigger null ref exceptions, faking that we can't reach the endpoint
            mock.Setup(fetcher => fetcher.ConstructWebRequest("www.dummy.com")).Returns((HttpWebRequest) null);

            var s3FileFetcher = new S3FileFetcher(mock.Object);
            var stream = s3FileFetcher.OpenFileStream("ServiceEndPoints.xml", S3FileFetcher.CacheMode.Never);

            Assert.NotNull(stream);
            Assert.Equal(S3FileFetcher.ResolvedLocation.Resources, s3FileFetcher.ResolvedContentLocation);

            string content;
            using (var reader = new StreamReader(stream))
            {
                content = reader.ReadToEnd();
            }

            Assert.False(string.IsNullOrEmpty(content));

            // we should have got back a real document, so verify (we could do deeper checks here too on the real content)
            Assert.StartsWith("<?xml version=\"1.0\"", content);
        }

    }

}
