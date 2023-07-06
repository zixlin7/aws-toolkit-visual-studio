using System.Collections.Generic;

using Amazon.AWSToolkit.CommonValidators;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CommonValidators
{
    public class CfnStackStatusValidatorTests
    {
        private readonly Mock<IAmazonCloudFormation> _cfnClient = new Mock<IAmazonCloudFormation>();
        private readonly string _sampleStack = "testStack";

        public static IEnumerable<object[]> ValidStackData =>
            new List<object[]>
            {
                new object[] { StackStatus.CREATE_COMPLETE },
                new object[] { StackStatus.ROLLBACK_COMPLETE },
                new object[] { StackStatus.UPDATE_IN_PROGRESS },
            };

        [Theory]
        [MemberData(nameof(ValidStackData))]
        public void ValidStackStatus(StackStatus status)
        {
            SetupCfnClient(status);
            var value = CfnStackStatusValidator.Validate(_cfnClient.Object, status);
            Assert.Null(value);
        }

        public static IEnumerable<object[]> InvalidStackData =>
            new List<object[]>
            {
                new object[] { StackStatus.DELETE_FAILED },
                new object[] { StackStatus.UPDATE_ROLLBACK_FAILED },
                new object[] { StackStatus.ROLLBACK_FAILED },
                new object[] { StackStatus.UPDATE_FAILED }
            };


        [Theory]
        [MemberData(nameof(InvalidStackData))]
        public void InvalidStackStatus(StackStatus status)
        {
            SetupCfnClient(status);
            var value = CfnStackStatusValidator.Validate(_cfnClient.Object, status);
            Assert.NotNull(value);
        }

        [Fact]
        public void Validator_DoesNotThrowOnError()
        {
            _cfnClient.Setup(mock =>
                mock.DescribeStacks(It.IsAny<DescribeStacksRequest>())).Throws(new AmazonServiceException("error"));
            var value = CfnStackStatusValidator.Validate(_cfnClient.Object, StackStatus.UPDATE_ROLLBACK_FAILED);
            Assert.Null(value);
        }

        private void SetupCfnClient(StackStatus status)
        {
            var sampleStack = new Stack() { StackName = _sampleStack, StackStatus = status };
            var response = new DescribeStacksResponse() { Stacks = new List<Stack>() { sampleStack } };
            _cfnClient.Setup(mock =>
                mock.DescribeStacks(It.IsAny<DescribeStacksRequest>())).Returns(response);
        }
    }
}
