using System;

using Amazon;
using Amazon.AWSToolkit.CloudWatch.Nodes;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.CloudWatch;
using Amazon.CloudWatchLogs;
using Amazon.Runtime;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch
{
    public class CloudWatchRootViewModelTests
    {
        private readonly TestCloudWatchRootViewModel _rootViewModel;

        private readonly ToolkitContextFixture _context = new ToolkitContextFixture();

        private readonly ToolkitRegion _region =
            new ToolkitRegion { Id = "us-west-2", PartitionId = "aws", DisplayName = "US West (Oregon)" };

        private readonly ICredentialIdentifier _identifier = FakeCredentialIdentifier.Create("sample");

        private readonly Mock<IMetaNode> _metaNode = new Mock<IMetaNode>();

        public CloudWatchRootViewModelTests()
        {
            SetupServiceManagerWithCloudWatchClients();
            SetupMetaNode();
            _context.SetupExecuteOnUIThread();

            _rootViewModel =
                new TestCloudWatchRootViewModel(_metaNode.Object, null, _identifier, _region, _context.ToolkitContext);
        }


        [StaFact]
        public void LoadChildren()
        {
            _rootViewModel.ExposedLoadChildren();
            Assert.Single(_rootViewModel.Children);
            Assert.IsType<LogGroupsRootViewModel>(_rootViewModel.Children[0]);
        }

        [Fact]
        public void CreateCloudWatchClient()
        {
            var cloudWatchClient = _rootViewModel.CloudWatchClient;
            Assert.NotNull(cloudWatchClient);
            Assert.IsAssignableFrom<IAmazonCloudWatch>(cloudWatchClient);
        }

        [Fact]
        public void CreateCloudWatchLogsClient()
        {
            var cloudWatchLogsClient = _rootViewModel.CloudWatchLogsClient;
            Assert.NotNull(cloudWatchLogsClient);
            Assert.IsAssignableFrom<IAmazonCloudWatchLogs>(cloudWatchLogsClient);
        }


        private void SetupServiceManagerWithCloudWatchClients()
        {
            var cwClient =
                new Mock<AmazonCloudWatchClient>(new AnonymousAWSCredentials(),
                    new AmazonCloudWatchConfig() { RegionEndpoint = RegionEndpoint.USWest2 });
            var cwlClient =
                new Mock<AmazonCloudWatchLogsClient>(new AnonymousAWSCredentials(),
                    new AmazonCloudWatchLogsConfig() { RegionEndpoint = RegionEndpoint.USWest2 });

            _context.ServiceClientManager
                .Setup(mock =>
                    mock.CreateServiceClient<AmazonCloudWatchClient>(It.IsAny<ICredentialIdentifier>(),
                        It.IsAny<ToolkitRegion>()))
                .Returns(cwClient.Object);

            _context.ServiceClientManager
                .Setup(mock =>
                    mock.CreateServiceClient<AmazonCloudWatchLogsClient>(It.IsAny<ICredentialIdentifier>(),
                        It.IsAny<ToolkitRegion>()))
                .Returns(cwlClient.Object);
        }

        private void SetupMetaNode()
        {
            _metaNode.Setup(mock => mock.FindChild<LogGroupsRootViewMetaNode>())
                .Returns(new LogGroupsRootViewMetaNode());
        }
    }
}
