using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.ECS.DeploymentWorkers;
using System.Linq;
using Xunit;

namespace AWSToolkit.Tests.ECS
{
    public class DeployScheduleTaskWorkerTests
    {
        private readonly EcsDeployTestFixture _fixture = new EcsDeployTestFixture();

        private readonly DeployScheduleTaskWorker _sut;

        public DeployScheduleTaskWorkerTests()
        {
            _sut = new DeployScheduleTaskWorker(
                _fixture.DockerHelper.Object,
                _fixture.CloudWatchEventsClient.Object,
                _fixture.EcrClient.Object,
                _fixture.EcsClient.Object,
                _fixture.IamClient.Object,
                _fixture.CloudWatchLogsClient.Object,
                _fixture.TelemetryLogger.Object);
        }

        [Fact]
        public void DeployTaskWorkerRecordsSuccess()
        {
            _fixture.SetupEcsDeployToSucceed();
            _sut.Execute(_fixture.EcsDeployState, _fixture.EcsDeploy.Object);

            _fixture.AssertTelemetryRecordCalls(1);
            Assert.Single(_fixture.LoggedMetrics);

            AssertScheduledDeployMetrics(_fixture.LoggedMetrics.First(), Result.Succeeded);
        }

        [Fact]
        public void DeployTaskWorkerRecordsFailed()
        {
            _fixture.SetupEcsDeployToFail();
            _sut.Execute(_fixture.EcsDeployState, _fixture.EcsDeploy.Object);

            _fixture.AssertTelemetryRecordCalls(1);
            Assert.Single(_fixture.LoggedMetrics);

            AssertScheduledDeployMetrics(_fixture.LoggedMetrics.First(), Result.Failed);
        }

        [Fact]
        public void DeployTaskWorkerRecordsFailedWithException()
        {
            _fixture.SetupEcsDeployToThrow();
            _sut.Execute(_fixture.EcsDeployState, _fixture.EcsDeploy.Object);

            _fixture.AssertTelemetryRecordCalls(1);
            Assert.Single(_fixture.LoggedMetrics);

            AssertScheduledDeployMetrics(_fixture.LoggedMetrics.First(), Result.Failed);
        }

        private void AssertScheduledDeployMetrics(Metrics metrics, Result expectedResult)
        {
            Assert.Equal(1, metrics.Data.Count);

            var datum = metrics.Data.First();

            Assert.Equal("ecs_deployScheduledTask", datum.MetricName);
            Assert.Equal(expectedResult.ToString(), datum.Metadata["result"]);
            Assert.Equal(_fixture.EcsDeployState.Region.SystemName, datum.Metadata["regionId"]);
            Assert.Equal(EcsLaunchType.Fargate.ToString(), datum.Metadata["ecsLaunchType"]);
        }
    }
}