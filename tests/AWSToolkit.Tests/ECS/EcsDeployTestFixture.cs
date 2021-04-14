using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS;
using Amazon.AWSToolkit.ECS.DeploymentWorkers;
using Amazon.CloudWatchEvents;
using Amazon.CloudWatchLogs;
using Amazon.EC2;
using Amazon.ECR;
using Amazon.ECS;
using Amazon.ElasticLoadBalancingV2;
using Amazon.IdentityManagement;
using Moq;
using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;
using LaunchType = Amazon.ECS.LaunchType;

namespace AWSToolkit.Tests.ECS
{
    public class EcsDeployTestFixture
    {
        public readonly ToolkitContextFixture ToolkitContextFixture = new ToolkitContextFixture();
        public readonly List<Metrics> LoggedMetrics = new List<Metrics>();

        public readonly Mock<IDockerDeploymentHelper> DockerHelper = new Mock<IDockerDeploymentHelper>();
        public readonly Mock<IAmazonCloudWatchEvents> CloudWatchEventsClient = new Mock<IAmazonCloudWatchEvents>();
        public readonly Mock<IAmazonCloudWatchLogs> CloudWatchLogsClient = new Mock<IAmazonCloudWatchLogs>();
        public readonly Mock<IAmazonEC2> Ec2Client = new Mock<IAmazonEC2>();
        public readonly Mock<IAmazonECR> EcrClient = new Mock<IAmazonECR>();
        public readonly Mock<IAmazonECS> EcsClient = new Mock<IAmazonECS>();
        public readonly Mock<IAmazonElasticLoadBalancingV2> LoadBalancingClient = new Mock<IAmazonElasticLoadBalancingV2>();
        public readonly Mock<IAmazonIdentityManagementService> IamClient = new Mock<IAmazonIdentityManagementService>();

        public readonly Mock<IAWSWizard> AwsWizard = new Mock<IAWSWizard>();
        public readonly EcsDeployState EcsDeployState = new EcsDeployState();

        public readonly Mock<IEcsDeploy> EcsDeploy = new Mock<IEcsDeploy>();

        public EcsDeployTestFixture()
        {
            ToolkitContextFixture.TelemetryLogger.Setup(mock => mock.Record(It.IsAny<Metrics>()))
                .Callback<Metrics>(metrics =>
                {
                    LoggedMetrics.Add(metrics);
                });

            AwsWizard.SetupGet(mock => mock[PublishContainerToAWSWizardProperties.LaunchType])
                .Returns(LaunchType.FARGATE.Value);

            EcsDeployState.HostingWizard = AwsWizard.Object;
            EcsDeployState.Region = new ToolkitRegion()
            {
                Id = "us-east-1",
                DisplayName = "US East",
                PartitionId = PartitionIds.AWS,
            };
        }

        public void SetupEcsDeployToSucceed()
        {
            EcsDeploy.Setup(mock => mock.Deploy(It.IsAny<EcsDeployState>())).ReturnsAsync(true);
        }

        public void SetupEcsDeployToFail()
        {
            EcsDeploy.Setup(mock => mock.Deploy(It.IsAny<EcsDeployState>())).ReturnsAsync(false);
        }

        public void SetupEcsDeployToThrow()
        {
            EcsDeploy.Setup(mock => mock.Deploy(It.IsAny<EcsDeployState>()))
                .ThrowsAsync(new Exception("Simulation: Deployment failure"));
        }

        public void AssertTelemetryRecordCalls(int count)
        {
            ToolkitContextFixture.TelemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Exactly(count));
        }
    }
}
