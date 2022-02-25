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

        [Theory]
        [InlineData("rate(1 minute)", 1, "minute")]
        [InlineData("rate(88 minutes)", 88, "minutes")]
        [InlineData("rate(48 hours)", 48, "hours")]
        [InlineData("rate(500 days)", 500, "days")]
        public void TryParseRateExpressionsThatPass(string expr, int expectedValue, string expectedUnit)
        {
            SetupForScheduleExpressionTests(expr);

            Assert.True(_wizard.Object.TryParseRateExpression(out int actualValue, out string actualUnit));
            Assert.Equal(expectedValue, actualValue);
            Assert.Equal(expectedUnit, actualUnit);
        }

        [Theory]
        [InlineData("rate(x minute)")]
        [InlineData("rate(y hours)")]
        [InlineData("rate(60 Minutes")]
        [InlineData("rate(9.5 weeks)")]
        [InlineData("rate(12 monkeys)")]
        [InlineData("rats(4 days)")]
        [InlineData("cron(* * * * * *)")]
        [InlineData("KABOOM!!!")]
        [InlineData(null)]
        [InlineData(42)]
        public void TryParseRateExpressionsThatFail(object expr)
        {
            SetupForScheduleExpressionTests(expr);

            Assert.False(_wizard.Object.TryParseRateExpression(out _, out _));
        }

        [Theory]
        [InlineData("cron(0 12 * * ? *)")]
        [InlineData("cron(5,35 14 * * ? *)")]
        [InlineData("cron(15 10 ? * 6L 2019-2022)")]
        public void TryGetCronExpressionsThatPass(string expr)
        {
            SetupForScheduleExpressionTests(expr);

            Assert.True(_wizard.Object.TryGetCronExpression(out string actualCronExpr));
            Assert.Equal(expr, actualCronExpr);
        }

        [Theory]
        [InlineData("rate(1 minute)")]
        [InlineData("chron(* * * * * *)")]
        [InlineData("KABOOM!!!")]
        [InlineData(null)]
        [InlineData(42)]
        public void TryGetCronExpressionsThatFail(object expr)
        {
            SetupForScheduleExpressionTests(expr);

            Assert.False(_wizard.Object.TryGetCronExpression(out _));
        }

        private void SetupForScheduleExpressionTests(object expr)
        {
            string propName = PublishContainerToAWSWizardProperties.ScheduleExpression;

            _wizard.Setup(mock => mock.IsPropertySet(propName)).Returns(true);
            _wizard.Setup(mock => mock.GetProperty(propName)).Returns(expr);
        }
    }
}
