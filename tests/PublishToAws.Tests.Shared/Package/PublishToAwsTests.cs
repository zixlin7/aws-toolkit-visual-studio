using System.Collections.Generic;
using System.IO;
using System.Linq;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.Package;
using Amazon.AWSToolkit.Tests.Common.Versioning;
using Amazon.AWSToolkit.Tests.Publishing.Common;

using Moq;

using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.Tests.Publishing.Package
{
    public class PublishToAwsTests
    {
        private readonly PublishContextFixture _publishContextFixture = new PublishContextFixture();
        private readonly FakeDotNetVersionProvider _dotNetVersionProvider = new FakeDotNetVersionProvider();

        private readonly PublishToAws _sut;

        public PublishToAwsTests()
        {
            _sut = new PublishToAws(_publishContextFixture.PublishContext);
            _dotNetVersionProvider.MajorVersion = 6;
        }

#if VS2019
        [StaFact]
        public async Task ShouldHandleUnsupportedDotNet()
        {
            _dotNetVersionProvider.MajorVersion = 3;

            await AssertShowDocumentCancelledAsync();
        }
#endif

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

            var publishSetup = Assert.Single(GetPublishSetupMetrics(PublishSetupStage.Initialize));
            AssertFailedResult(publishSetup);
        }

        [StaFact]
        public async Task ShouldHandleGetRestClientFailure()
        {
            _publishContextFixture.StubCliServerGetRestClientToThrow();

            await AssertShowDocumentFailsAsync();

            var publishSetup = Assert.Single(GetPublishSetupMetrics(PublishSetupStage.Show));
            AssertFailedResult(publishSetup);
        }

        [StaFact]
        public async Task ShouldHandleGetDeploymentClientFailure()
        {
            _publishContextFixture.StubCliServerGetDeploymentClientToThrow();

            await AssertShowDocumentFailsAsync();

            var publishSetup = Assert.Single(GetPublishSetupMetrics(PublishSetupStage.Show));
            AssertFailedResult(publishSetup);
        }

        private async Task AssertShowDocumentFailsAsync()
        {
            var args = new ShowPublishToAwsDocumentArgs();
            await _sut.ShowPublishToAwsDocumentAsync(args, _dotNetVersionProvider);

            _publishContextFixture.ToolkitShellProvider.Verify(
                mock => mock.ShowMessage("Unable to Publish to AWS", It.IsAny<string>()), Times.Once);

            var publishStart = Assert.Single(_publishContextFixture.TelemetryFixture.GetMetricsByMetricName("publish_start"));
            AssertFailedResult(publishStart);

            var publishSetup = Assert.Single(GetPublishSetupMetrics(PublishSetupStage.All));
            AssertFailedResult(publishSetup);
        }

        private async Task AssertShowDocumentCancelledAsync()
        {
            var args = new ShowPublishToAwsDocumentArgs();
            await _sut.ShowPublishToAwsDocumentAsync(args, _dotNetVersionProvider);

            var publishStart = Assert.Single(_publishContextFixture.TelemetryFixture.GetMetricsByMetricName("publish_start"));
            AssertCancelledResult(publishStart);

            var publishSetup = Assert.Single(GetPublishSetupMetrics(PublishSetupStage.All));
            AssertCancelledResult(publishSetup);
        }

        private static void AssertFailedResult(Metrics publishStart)
        {
            Assert.Contains(publishStart.Data, d => d.Metadata["result"] == Result.Failed.ToString());
        }

        private static void AssertCancelledResult(Metrics publishStart)
        {
            Assert.Contains(publishStart.Data, d => d.Metadata["result"] == Result.Cancelled.ToString());
        }

        private IList<Metrics> GetPublishSetupMetrics(PublishSetupStage publishSetupStage)
        {
            return _publishContextFixture.TelemetryFixture.GetMetricsByMetricName("publish_setup")
                .Where(metrics => metrics.Data.Any(datum => datum.Metadata["publishSetupStage"] == publishSetupStage.ToString()))
                .ToList();
        }
    }
}
