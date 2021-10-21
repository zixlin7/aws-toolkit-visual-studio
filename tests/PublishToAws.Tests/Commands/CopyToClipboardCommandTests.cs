using System.Collections.Generic;
using System.Collections.ObjectModel;

using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class CopyToClipboardCommandTests
    {

        public static IEnumerable<object[]> PublishResourceData = new List<object[]>
        {
            new object[] {null}, new object[] {new ObservableCollection<PublishResource>()},
        };

        [Fact]
        public void CreateContent()
        {
            var publishResources = CreateSamplePublishResource();
            var output = CopyToClipboardCommand.CreateContent(publishResources);
            var expected = CreateExpectedContent(publishResources);
            Assert.Equal(expected, output);
        }


        [Theory]
        [MemberData(nameof(PublishResourceData))]
        public void CreateContent_WhenEmpty(ObservableCollection<PublishResource> publishResources)
        {
            var output = CopyToClipboardCommand.CreateContent(publishResources);
            Assert.True(string.IsNullOrEmpty(output));
        }

        private ObservableCollection<PublishResource> CreateSamplePublishResource()
        {
            var data = new Dictionary<string, string>() {{"Endpoint", "http://test-dev-endpoint.com/"}};
            var publishResource = new PublishResource("test-dev",
                "AWS::ElasticBeanstalk::Environment",
                "Application Endpoint",
                data);
            return new ObservableCollection<PublishResource>() {publishResource};
        }

        private string CreateExpectedContent(ObservableCollection<PublishResource> publishResources)
        {
            var expected = $"Application Endpoint\r\n" +
                                 $"AWS::ElasticBeanstalk::Environment\r\n" +
                                 $"test-dev\r\n" +
                                 $"Endpoint:\r\n" +
                                 $"http://test-dev-endpoint.com/\r\n" +
                                 $"\r\n";

            return expected;

        }
    }
}
