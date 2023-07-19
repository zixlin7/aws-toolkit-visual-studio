using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Exceptions;
using Amazon.CloudFormation;
using Amazon.EC2;
using Amazon.Lambda;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SSOOIDC;

using Xunit;


namespace Amazon.AWSToolkit.Util.Tests
{
    public class ExceptionExtensionMethodsTests
    {
        public static IEnumerable<object[]> ValidServiceExceptions = new List<object[]>
        {
            new object[] { new AmazonS3Exception("test"), "S3" },
            new object[] { new AmazonCloudFormationException("test"), "CloudFormation" },
            new object[] { new AmazonLambdaException("test"), "Lambda" },
            new object[] { new AmazonEC2Exception("test"), "EC2" },
            new object[] { new AmazonSSOOIDCException("test"), "SSOOIDC" },
        };


        [Theory]
        [MemberData(nameof(ValidServiceExceptions))]
        public void GetServiceName(Exception ex, string expectedServiceName)
        {
            Assert.Equal(expectedServiceName, ex.GetServiceName());
        }


        public static IEnumerable<object[]> InvalidExceptions = new List<object[]>
        {
            new object[] { new ArgumentNullException("test") },
            new object[] { new InvalidOperationException("test") },
            new object[] { new Exception("test") },
            new object[]
            {
                new LambdaToolkitException("test", LambdaToolkitException.LambdaErrorCode.LambdaCreateFunction)
            },
            new object[] { new StackOverflowException("test") },
            new object[] { new AmazonUnmarshallingException("abced", "test-location", null) },
        };

        [Theory]
        [MemberData(nameof(InvalidExceptions))]
        public void GetServiceName_Invalid(Exception ex)
        {
            Assert.Null(ex.GetServiceName());
        }

        public static IEnumerable<object[]> CustomServiceExceptions = new List<object[]>
        {
            new object[] { new ECS.Model.AccessDeniedException("test"), "ECS" },
            new object[] { new CloudWatchLogs.Model.ResourceNotFoundException("test"), "CloudWatchLogs" },
            new object[] { new ECR.Model.InvalidParameterException("test"), "ECR" },
        };


        [Theory]
        [MemberData(nameof(CustomServiceExceptions))]
        public void GetServiceName_CustomServiceException(Exception ex, string expectedServiceName)
        {
            Assert.Equal(expectedServiceName, ex.GetServiceName());
        }
    }
}
