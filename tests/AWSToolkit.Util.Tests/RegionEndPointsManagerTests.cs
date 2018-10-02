using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Xunit;
using Amazon.AWSToolkit;
using Moq;
using Amazon.AWSToolkit.MobileAnalytics;
using AWSToolkit.Util.Tests;

namespace Amazon.AWSToolkit.Util.Tests
{
    public class RegionEndPointsManagerTests : IDisposable
    {
        private string _testWorkspaceFolder;

        public RegionEndPointsManagerTests()
        {
            /// Each test gets its own random subfolder to use.
            /// Folder is auto cleaned up at the end of the test, and allows tests to run in parallel.
            _testWorkspaceFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_testWorkspaceFolder);
        }

        /// <summary>
        /// Validates that the endpoints available in cache or from download
        /// load successfully - this is effectively a canary for the normal user 
        /// experience.
        /// </summary>
        [Fact]
        public void TestManagerCanUseProductionEndpoints()
        {
            // no custom file fetcher means use of the default probing for production endpoint data -
            // the 'happy path' test
            var rep = RegionEndPointsManager.GetInstance(); 

            rep.Refresh();
            Assert.False(rep.FailedToLoad, "FailedToLoad flag set true, an error occurred");

            // validate we can enumerate and retrieve by name
            foreach (var r in rep.Regions)
            {
                var endpoint = rep.GetRegion(r.SystemName);
                Assert.NotNull(endpoint);

                // validate that if we find ECS, ECR is also present in a region
                var ecsEndpoint = endpoint.GetEndpoint(RegionEndPointsManager.ECS_SERVICE_NAME);
                if (ecsEndpoint != null)
                {
                    Assert.NotNull(endpoint.GetEndpoint(RegionEndPointsManager.ECR_SERVICE_NAME));
                }
                else
                {
                    Assert.Null(endpoint.GetEndpoint(RegionEndPointsManager.ECR_SERVICE_NAME));
                }

                // reverse last test and verify if ECR present, so too is ECS; this isn't a problem
                // that causes trouble for the toolkit since we gate on ECS but could indicate a bad
                // endpoints file that would be nice to know about
                var ecrEndpoint = endpoint.GetEndpoint(RegionEndPointsManager.ECR_SERVICE_NAME);
                if (ecrEndpoint != null)
                {
                    Assert.NotNull(endpoint.GetEndpoint(RegionEndPointsManager.ECS_SERVICE_NAME));
                }
            }
        }

        [Fact]
        public void TestLocalRegionEndPointsFileFetcherInjection()
        {
            var s3FileFetcher = new S3FileFetcher();
            var rep = RegionEndPointsManager.GetInstance(s3FileFetcher);
            var localRegion = rep.Regions.OfType<RegionEndPointsManager.LocalRegionEndPoints>().Single();
            Assert.Equal(s3FileFetcher, localRegion.FileFetcher);
        }

        [Fact]
        public void TestManagerRollsbackToResourcesOnCorruptedEndpointsXml()
        {
            string badEndpointsXml;
            using (var reader = new StreamReader(TestUtil.LoadInvalidXmlEndpointsFile("InvalidXmlEndpointsFile.xml")))
            {
                badEndpointsXml = reader.ReadToEnd();
            }

            // use a random subfolder so tests can run in parallel
            var dummyEndpointsPath = Path.Combine(_testWorkspaceFolder, Constants.SERVICE_ENDPOINT_FILE);
            File.WriteAllText(dummyEndpointsPath, badEndpointsXml);

            var mock = new Mock<IS3FileFetcherContentResolver>();
            mock.Setup(fetcher => fetcher.GetUserConfiguredLocalHostedFilesPath()).Returns(_testWorkspaceFolder);

            var s3FileFetcher = new S3FileFetcher(mock.Object);
            var rep = RegionEndPointsManager.GetInstance(s3FileFetcher);

            Assert.False(rep.FailedToLoad, "FailedToLoad flag set true, an error occurred");
            Assert.True(rep.LoadedFromResources, "Expected LoadedFromResources to be set true!");
            Assert.Equal(S3FileFetcher.ResolvedLocation.Resources, s3FileFetcher.ResolvedContentLocation);

            // validate we can enumerate and retrieve by name regions that should have been loaded
            // from resources
            foreach (var r in rep.Regions)
            {
                var endpoint = rep.GetRegion(r.SystemName);
                Assert.NotNull(endpoint);
            }
        }

        /// <summary>
        /// If the Endpoints Config file is not available locally, S3FileFetcher attempts to load from CloudFront, then S3.
        /// Simulate a CloudFront failure, and check that a url failure Metric was sent.
        /// </summary>
        [Fact]
        public void TestManagerProducesMetricsForCloudFrontFailure()
        {
            var s3FileFetcherContentResolverMock = new Mock<IS3FileFetcherContentResolver>();
            s3FileFetcherContentResolverMock.Setup(fetcher => fetcher.GetUserConfiguredLocalHostedFilesPath()).Returns(_testWorkspaceFolder); // no hosted file exists, triggering url loads
            s3FileFetcherContentResolverMock.Setup(fetcher => fetcher.ConstructWebRequest(GetCloudFrontConfigFilesLocation())).Throws<Exception>(); // Simulate Cloudfront retrieval failure
            s3FileFetcherContentResolverMock.Setup(fetcher => fetcher.ConstructWebRequest(GetS3FallbackConfigFilesLocation())).Returns<string>(url => WebRequest.Create(url) as HttpWebRequest); // Allow S3 requests to work

            var simpleMobileAnalyticsMock = new Mock<ISimpleMobileAnalytics>();

            var s3FileFetcher = new S3FileFetcher(s3FileFetcherContentResolverMock.Object, simpleMobileAnalyticsMock.Object);

            // Create our own Endpoints Manager, because we have a custom file fetcher, which we don't want 
            // getting stuffed into the Singleton Instance and breaking other tests.
            var rep = RegionEndPointsManagerTestExtension.CreateRegionEndPointsManager(s3FileFetcher);

            Assert.False(rep.FailedToLoad, "FailedToLoad flag set true, an error occurred");
            Assert.False(rep.LoadedFromResources, "Expected LoadedFromResources to be set false!");
            Assert.Equal(S3FileFetcher.ResolvedLocation.S3, s3FileFetcher.ResolvedContentLocation);

            // Assert that we logged a url load error
            // It fires two times because CacheFileContent fails, triggering a second OpenFileStream within S3FileFetcher
            simpleMobileAnalyticsMock.Verify(mock => mock.QueueEventToBeRecorded(It.Is<ToolkitEvent>(toolkitEvent => toolkitEvent.Attributes.ContainsKey(AttributeKeys.FileFetcherUrlFailure.ToString()))), Times.Exactly(2), "A Metric indicating a load from url failure should have been sent");
        }

        /// <summary>
        /// If the Endpoints Config file is not available locally, S3FileFetcher attempts to load from CloudFront, then S3.
        /// Simulate a CloudFront and S3 failure, and check that a url failure Metric was sent.
        /// </summary>
        [Fact]
        public void TestManagerProducesMetricsForCloudFrontAndS3Failures()
        {
            var s3FileFetcherContentResolverMock = new Mock<IS3FileFetcherContentResolver>();
            s3FileFetcherContentResolverMock.Setup(fetcher => fetcher.GetUserConfiguredLocalHostedFilesPath()).Returns(_testWorkspaceFolder); // no hosted file exists, triggering url loads
            s3FileFetcherContentResolverMock.Setup(fetcher => fetcher.ConstructWebRequest(It.IsAny<string>())).Throws<Exception>(); // Simulate retrieval failure from any url

            var simpleMobileAnalyticsMock = new Mock<ISimpleMobileAnalytics>();

            var s3FileFetcher = new S3FileFetcher(s3FileFetcherContentResolverMock.Object, simpleMobileAnalyticsMock.Object);

            // Create our own Endpoints Manager, because we have a custom file fetcher, which we don't want 
            // getting stuffed into the Singleton Instance and breaking other tests.
            var rep = RegionEndPointsManagerTestExtension.CreateRegionEndPointsManager(s3FileFetcher);

            Assert.False(rep.FailedToLoad, "FailedToLoad flag set true, an error occurred");
            Assert.False(rep.LoadedFromResources, "Expected LoadedFromResources to be set false!");
            Assert.Equal(S3FileFetcher.ResolvedLocation.Resources, s3FileFetcher.ResolvedContentLocation);

            // Assert that we logged telemetry
            simpleMobileAnalyticsMock.Verify(mock => mock.QueueEventToBeRecorded(It.Is<ToolkitEvent>(toolkitEvent => toolkitEvent.Attributes.ContainsKey(AttributeKeys.FileFetcherUrlFailure.ToString()))), Times.Exactly(2), "A Metric indicating a load form url failure should have been sent");
        }

        #region IDisposable 

        public void Dispose()
        {
            if (Directory.Exists(_testWorkspaceFolder))
            {
                Directory.Delete(_testWorkspaceFolder, true);
            }
        }

        #endregion

        private string GetCloudFrontConfigFilesLocation()
        {
            return string.Format("{0}{1}", S3FileFetcher.CLOUDFRONT_CONFIG_FILES_LOCATION, Constants.SERVICE_ENDPOINT_FILE);
        }

        private string GetS3FallbackConfigFilesLocation()
        {
            return string.Format("{0}{1}", S3FileFetcher.S3_FALLBACK_LOCATION, Constants.SERVICE_ENDPOINT_FILE);
        }
    }
}
