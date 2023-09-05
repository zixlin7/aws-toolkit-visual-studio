using System;
using System.Collections.Generic;
using System.Net;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.CloudFormation;
using Amazon.Common.DotNetCli.Tools;
using Amazon.ECS.Model;

using Xunit;

using Amazon.Runtime;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    public class TelemetryHelperTests
    {
        private readonly AmazonServiceException _sampleServiceException = new AmazonCloudFormationException(
            "sample cfn error", ErrorType.Receiver,
            "ValidationError", "1234", HttpStatusCode.BadRequest);


        [Fact]
        public void ErrorMetadataHasUnknownReason_WhenExceptionNull()
        {
            var data = TelemetryHelper.DetermineErrorMetadata(null);

            Assert.NotNull(data);
            Assert.Equal(TelemetryHelper.UnknownReason, data.Reason);
            Assert.Null(data.HttpStatusCode);
        }

        [Fact]
        public void ErrorMetadata_WhenNoInnerException()
        {
            var data = TelemetryHelper.DetermineErrorMetadata(CreateSampleToolkitException(null));

            Assert.NotNull(data);
            Assert.Equal("ToolkitException", data.Reason);
            Assert.Equal("UnsupportedState", data.ErrorCode);
            Assert.Null(data.HttpStatusCode);
            Assert.Equal(CausedBy.Unknown, data.CausedBy);
            Assert.Null(data.RequestId);
            Assert.Null(data.RequestServiceType);
        }

        [Fact]
        public void ErrorMetadataParsesImmediateInnerException_WhenNestedServiceException()
        {
            var exception = CreateSampleToolkitException(_sampleServiceException);
            var data = TelemetryHelper.DetermineErrorMetadata(exception);

            Assert.NotNull(data);
            Assert.Equal("ToolkitException-AmazonCloudFormationException", data.Reason);
            Assert.Equal("UnsupportedState-ValidationError", data.ErrorCode);
            Assert.Equal(HttpStatusCode.BadRequest.ToString(), data.HttpStatusCode);
            Assert.Equal(CausedBy.Service, data.CausedBy);
            Assert.Equal("1234", data.RequestId);
            Assert.Equal("CloudFormation", data.RequestServiceType);
        }

        [Fact]
        public void ErrorMetadataSkipsDeeperInnerException_WhenNestedServiceException()
        {
            var innerException = CreateSampleToolkitException(_sampleServiceException);
            var exception = new ToolsException("Sample Tools Error", ToolsException.CommonErrorCode.DotnetPublishFailed,
                innerException);
            var data = TelemetryHelper.DetermineErrorMetadata(exception);

            Assert.NotNull(data);
            Assert.Equal("ToolsException-ToolkitException", data.Reason);
            Assert.Equal("DotnetPublishFailed-UnsupportedState-ValidationError", data.ErrorCode);
            Assert.Equal(HttpStatusCode.BadRequest.ToString(), data.HttpStatusCode);
            Assert.Equal(CausedBy.Unknown, data.CausedBy);
            Assert.Null(data.RequestId);
            Assert.Null(data.RequestServiceType);
        }


        [Fact]
        public void ErrorMetadataSkipsDeeperInnerException_WhenNestedSystemException()
        {
            var serviceException = new ResourceNotFoundException("ecsError", new ArgumentException("innermost error"),
                ErrorType.Receiver,
                "ValidationError", "1234", HttpStatusCode.BadRequest);
            var exception = CreateSampleToolkitException(serviceException);
            var data = TelemetryHelper.DetermineErrorMetadata(exception);

            Assert.NotNull(data);
            Assert.Equal("ToolkitException-ResourceNotFoundException", data.Reason);
            Assert.Equal("UnsupportedState-ValidationError", data.ErrorCode);
            Assert.Equal(HttpStatusCode.BadRequest.ToString(), data.HttpStatusCode);
            Assert.Equal(CausedBy.Service, data.CausedBy);
            Assert.Equal("1234", data.RequestId);
            Assert.Equal("ECS", data.RequestServiceType);
        }

        [Fact]
        public void ErrorMetadataParsesServiceValues_WhenNestedToolsServiceException()
        {
            var innerToolsException = new ToolsException("sample tools exception",
                ToolsException.CommonErrorCode.DotnetPublishFailed, _sampleServiceException);
            var exception = CreateSampleToolkitException(innerToolsException);

            var data = TelemetryHelper.DetermineErrorMetadata(exception);

            Assert.NotNull(data);
            Assert.Equal("ToolkitException-ToolsException", data.Reason);
            Assert.Equal("UnsupportedState-DotnetPublishFailed-ValidationError", data.ErrorCode);
            Assert.Equal(HttpStatusCode.BadRequest.ToString(), data.HttpStatusCode);
            Assert.Equal(CausedBy.Unknown, data.CausedBy);
            Assert.Null(data.RequestId);
            Assert.Null(data.RequestServiceType);
        }

        public static IEnumerable<object[]> CausedByData = new List<object[]>
        {
            new object[] { ErrorType.Unknown, CausedBy.Unknown },
            new object[] { ErrorType.Receiver, CausedBy.Service },
            new object[] { ErrorType.Sender, CausedBy.Unknown },
            new object[] { null, CausedBy.Unknown }
        };


        [Theory]
        [MemberData(nameof(CausedByData))]
        public void ErrorMetadata_VerifyCausedBy(ErrorType errorType, CausedBy expectedCausedBy)
        {
            _sampleServiceException.ErrorType = errorType;

            var exception = CreateSampleToolkitException(_sampleServiceException);
            var data = TelemetryHelper.DetermineErrorMetadata(exception);

            Assert.NotNull(data);
            Assert.Equal(expectedCausedBy, data.CausedBy);
        }

        private Exception CreateSampleToolkitException(Exception serviceException)
        {
            return new ToolkitException("sample toolkit error", ToolkitException.CommonErrorCode.UnsupportedState,
                serviceException);
        }
    }
}
