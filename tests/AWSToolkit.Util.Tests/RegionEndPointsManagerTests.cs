using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using Xunit;
using Amazon.AWSToolkit;
using Moq;

namespace AWSToolkit.Util.Tests
{
    public class RegionEndPointsManagerTests
    {
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
        public void TestManagerRollsbackToResourcesOnCorruptedEndpointsXml()
        {
            string badEndpointsXml;
            using (var reader = new StreamReader(TestUtil.LoadInvalidXmlEndpointsFile("InvalidXmlEndpointsFile.xml")))
            {
                badEndpointsXml = reader.ReadToEnd();
            }

            var tempLocation = Path.Combine(Path.GetTempPath(), TestUtil.TestFileFolder);
            Directory.CreateDirectory(tempLocation);
            var dummyEndpointsPath = Path.Combine(tempLocation, Constants.SERVICE_ENDPOINT_FILE);
            File.WriteAllText(dummyEndpointsPath, badEndpointsXml);

            var mock = new Mock<IS3FileFetcherContentResolver>();
            mock.Setup(fetcher => fetcher.GetUserConfiguredLocalHostedFilesPath()).Returns(tempLocation);

            try
            {
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
            finally
            {
                if (Directory.Exists(tempLocation))
                {
                    Directory.Delete(tempLocation, true);
                }
            }
        }
    }
}
