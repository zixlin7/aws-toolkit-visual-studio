using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.ECS.DeploymentWorkers;
using System.Linq;
using Xunit;

namespace AWSToolkit.Tests.ECS
{
    public class PushImageToECRWorkerTests
    {
        private readonly EcsDeployTestFixture _fixture = new EcsDeployTestFixture();

        private readonly PushImageToECRWorker _sut;

        public PushImageToECRWorkerTests()
        {
            _sut = new PushImageToECRWorker(
                _fixture.DockerHelper.Object,
                _fixture.EcrClient.Object,
                _fixture.ToolkitContextFixture.ToolkitContext);
        }

        [Fact]
        public void PushImageRecordsSuccess()
        {
            _fixture.SetupEcsDeployToSucceed();
            _sut.Execute(_fixture.EcsDeployState, _fixture.EcsDeploy.Object);

            _fixture.AssertTelemetryRecordCalls(1);
            Assert.Single(_fixture.LoggedMetrics);

            AssertDeployMetrics(_fixture.LoggedMetrics.First(), Result.Succeeded);
        }

        [Fact]
        public void PushImageRecordsFailed()
        {
            _fixture.SetupEcsDeployToFail();
            _sut.Execute(_fixture.EcsDeployState, _fixture.EcsDeploy.Object);

            _fixture.AssertTelemetryRecordCalls(1);
            Assert.Single(_fixture.LoggedMetrics);

            AssertDeployMetrics(_fixture.LoggedMetrics.First(), Result.Failed);
        }

        [Fact]
        public void PushImageRecordsFailedWithException()
        {
            _fixture.SetupEcsDeployToThrow();
            _sut.Execute(_fixture.EcsDeployState, _fixture.EcsDeploy.Object);

            _fixture.AssertTelemetryRecordCalls(1);
            Assert.Single(_fixture.LoggedMetrics);

            AssertDeployMetrics(_fixture.LoggedMetrics.First(), Result.Failed);
        }

        private void AssertDeployMetrics(Metrics metrics, Result expectedResult)
        {
            Assert.Equal(1, metrics.Data.Count);

            var datum = metrics.Data.First();

            Assert.Equal("ecr_deployImage", datum.MetricName);
            Assert.Equal(expectedResult.ToString(), datum.Metadata["result"]);
            Assert.Equal(_fixture.EcsDeployState.Region.Id, datum.Metadata["regionId"]);
        }
    }
}
