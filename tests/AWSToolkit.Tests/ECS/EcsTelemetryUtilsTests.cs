using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS;
using Amazon.AWSToolkit.ECS.Util;
using Amazon.ECS;
using Moq;
using System.Linq;
using System.Reflection;
using Xunit;

namespace AWSToolkit.Tests.ECS
{
    public class EcsTelemetryUtilsTests
    {
        private readonly Mock<IAWSWizard> _wizard = new Mock<IAWSWizard>();

        [Fact]
        public void GetMetricsEcsLaunchType_Ec2()
        {
            SetWizardLaunchType(LaunchType.EC2.Value);
            Assert.Equal(EcsLaunchType.Ec2 , EcsTelemetryUtils.GetMetricsEcsLaunchType(_wizard.Object));
        }

        [Fact]
        public void GetMetricsEcsLaunchType_Fargate()
        {
            SetWizardLaunchType(LaunchType.FARGATE.Value);
            Assert.Equal(EcsLaunchType.Fargate, EcsTelemetryUtils.GetMetricsEcsLaunchType(_wizard.Object));
        }

        /// <summary>
        /// This test notifies us (through failure) that future additions to ECS Launch types are not
        /// mapped to Metrics, and would be reported as unknown types.
        /// </summary>
        [Fact]
        public void EnsureLaunchTypesHandled()
        {
            var expectedMetricsTypes = typeof(EcsLaunchType).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(field => field.GetValue(null))
                .ToArray();

            var ec2LaunchTypeField = typeof(LaunchType).GetFields(BindingFlags.Static | BindingFlags.Public);
            
            ec2LaunchTypeField.ToList().ForEach(field =>
            {
                var ec2LaunchType = field.GetValue(null) as LaunchType;
                Assert.NotNull(ec2LaunchType);
                SetWizardLaunchType(ec2LaunchType.Value);

                Assert.True(expectedMetricsTypes.Contains(EcsTelemetryUtils.GetMetricsEcsLaunchType(_wizard.Object)),
                    $"Unhandled EC2 Launch Type. Metrics would be emitted as unknown type: {ec2LaunchType.Value}");
            });
        }

        private void SetWizardLaunchType(object launchType)
        {
            _wizard.SetupGet(mock => mock[PublishContainerToAWSWizardProperties.LaunchType]).Returns(launchType);
        }
    }
}