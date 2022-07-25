using System.Collections.Generic;

using Amazon.AWSToolkit.Publish.Models;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class DisplayedResourceSummaryExtensionMethodTests
    {
        [Fact]
        public void AsPublishResource()
        {
            var sampleDictionary = new Dictionary<string, string>() {{"sampleKey", "sampleValue"}};
            var summary = new DisplayedResourceSummary()
            {
                Id = "id-1",
                Type = "Application Endpoint",
                Description = "sample description",
                Data = sampleDictionary
            };

            var expectedResource =
                new PublishResource("id-1", "Application Endpoint", "sample description", sampleDictionary);

            var actualResource = summary.AsPublishResource();

            Assert.Equal(expectedResource, actualResource);
        }

        [Fact]
        public void AsPublishResource_PropertiesNull()
        {
            var summary = new DisplayedResourceSummary();
            var expectedResource = new PublishResource(null, null, null, null);

            var actualResource = summary.AsPublishResource();

            Assert.Equal(expectedResource, actualResource);
        }
    }
}
