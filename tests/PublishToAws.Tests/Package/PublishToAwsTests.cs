using System.IO;

using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.Package;
using Amazon.AWSToolkit.Publish.Services;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Publishing.Common;

using Moq;

using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.Tests.Publishing.Package
{
    public class PublishToAwsTests
    {
        private readonly PublishContextFixture _publishContextFixture = new PublishContextFixture();
        private readonly Mock<ICliServer> _cliServer = new Mock<ICliServer>();

        private readonly PublishToAws _sut;

        public PublishToAwsTests()
        {
            _sut = new PublishToAws(_publishContextFixture.PublishContext);
        }

        [StaFact]
        public async Task ShouldHandleInstallFailure()
        {
            _publishContextFixture.PublishContext.InitializeCliTask = Task.FromException(new IOException());

            await AssertShowDocumentFailsAsync();
        }

        [StaFact]
        public async Task ShouldHandleDeployToolStartupFailure()
        {
            _publishContextFixture.StubCliServerStartAsyncToThrow();

            await AssertShowDocumentFailsAsync();
        }

        [StaFact]
        public async Task ShouldHandleGetRestClientFailure()
        {
            _publishContextFixture.StubCliServerGetRestClientToThrow();

            await AssertShowDocumentFailsAsync();
        }

        [StaFact]
        public async Task ShouldHandleGetDeploymentClientFailure()
        {
            _publishContextFixture.StubCliServerGetDeploymentClientToThrow();

            await AssertShowDocumentFailsAsync();
        }

        private async Task AssertShowDocumentFailsAsync()
        {
            var args = new ShowPublishToAwsDocumentArgs();
            await _sut.ShowPublishToAwsDocument(args);

            _publishContextFixture.ToolkitShellProvider.Verify(
                mock => mock.ShowMessage("Unable to Publish to AWS", It.IsAny<string>()), Times.Once);

            var metrics = Assert.Single(_publishContextFixture.TelemetryFixture.LoggedMetrics);
            Assert.Contains(metrics.Data,
                d => d.MetricName == "publish_start" && d.Metadata["result"] == Result.Failed.ToString());
        }
    }
}
