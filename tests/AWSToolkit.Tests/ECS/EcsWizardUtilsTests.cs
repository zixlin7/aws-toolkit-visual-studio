using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS;
using Amazon.AWSToolkit.ECS.WizardPages;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.ECS
{
    public class EcsWizardUtilsTests
    {
        private readonly Mock<IAWSWizard> _wizard = new Mock<IAWSWizard>();

        [Fact]
        public void IsEc2Launch()
        {
            AssertIsEc2LaunchResult(false, null);
            AssertIsEc2LaunchResult(false, "");
            AssertIsEc2LaunchResult(false, "cookies");
            AssertIsEc2LaunchResult(false, Amazon.ECS.LaunchType.FARGATE.Value);
            AssertIsEc2LaunchResult(true, Amazon.ECS.LaunchType.EC2.Value);
            AssertIsEc2LaunchResult(true, Amazon.ECS.LaunchType.EC2.Value.ToLower());
        }

        [Fact]
        public void IsFargateLaunch()
        {
            AssertIsFargateLaunchResult(false, null);
            AssertIsFargateLaunchResult(false, "");
            AssertIsFargateLaunchResult(false, "cookies");
            AssertIsFargateLaunchResult(false, Amazon.ECS.LaunchType.EC2.Value);
            AssertIsFargateLaunchResult(true, Amazon.ECS.LaunchType.FARGATE.Value);
            AssertIsFargateLaunchResult(true, Amazon.ECS.LaunchType.FARGATE.Value.ToLower());
        }

        private void AssertIsEc2LaunchResult(bool expectedResult, object propertyValue)
        {
            _wizard.SetupGet(mock => mock[PublishContainerToAWSWizardProperties.LaunchType]).Returns(propertyValue);
            Assert.Equal(expectedResult, _wizard.Object.IsEc2Launch());
        }

        private void AssertIsFargateLaunchResult(bool expectedResult, object propertyValue)
        {
            _wizard.SetupGet(mock => mock[PublishContainerToAWSWizardProperties.LaunchType]).Returns(propertyValue);
            Assert.Equal(expectedResult, _wizard.Object.IsFargateLaunch());
        }
    }
}