using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.CloudWatch;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.Lambda;
using Amazon.Lambda.Model;

using AWSToolkit.Tests.CloudWatch.Logs.Util;

using Moq;

using Xunit;

using static System.Windows.Visibility;

using Architecture = Amazon.Lambda.Architecture;

namespace AWSToolkit.Tests.Lambda
{
    public class ViewFunctionControllerTests
    {
        private static readonly string SampleImageUri = "some-repo-uri";

        private readonly Mock<IAmazonLambda> _lambda = new Mock<IAmazonLambda>();
        private readonly ViewFunctionController _controller;
        private readonly GetFunctionConfigurationResponse _getPendingResponse;
        private readonly GetFunctionConfigurationResponse _getActiveResponse;
        private readonly GetFunctionConfigurationResponse _getInitialResponse;
        private readonly GetFunctionConfigurationResponse _packageTypeImageResponse;
        private readonly UpdateFunctionConfigurationResponse _updateProgressResponse;
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<ILogStreamsViewer> _logStreamsViewer = new Mock<ILogStreamsViewer>();

        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitHost;

        public static IEnumerable<object[]> ArchitectureData = new List<object[]>
        {
            new object[] {new List<string>{Architecture.Arm64, Architecture.X86_64}, $"{Architecture.Arm64},{Architecture.X86_64}"},
            new object[] {new List<string>{Architecture.Arm64}, $"{Architecture.Arm64}"},
            new object[] {new List<string>(), "None Found"},
            new object[] {null, "None Found"}
        };

        public ViewFunctionControllerTests()
        {
            var connectionSettings = new AwsConnectionSettings(null, null);
            _controller = new ViewFunctionController("mockFunctionName", "mockFunctionArn", _contextFixture.ToolkitContext, connectionSettings);
            _getPendingResponse = new GetFunctionConfigurationResponse
            {
                State = State.Pending,
                StateReasonCode = StateReasonCode.Creating,
                StateReason = "The function is being created",
                LastUpdateStatus = LastUpdateStatus.Successful,
                LastUpdateStatusReason = null,
                LastUpdateStatusReasonCode = null,
                CodeSize = Int64.MinValue,
                LastModified = "2015-12-11T12:28:30.45Z"
            };

            _updateProgressResponse = new UpdateFunctionConfigurationResponse
            {
                State = State.Active,
                LastUpdateStatus = LastUpdateStatus.InProgress,
                LastUpdateStatusReason = "The function is being created",
                LastUpdateStatusReasonCode = LastUpdateStatusReasonCode.EniLimitExceeded,
                LastModified = "2015-12-11T12:28:30.45Z"
            };

            _getActiveResponse = new GetFunctionConfigurationResponse
            {
                State = State.Active,
                LastUpdateStatus = LastUpdateStatus.Successful,
                LastModified = "2015-12-11T12:28:30.45Z"
            };

            _getInitialResponse = new GetFunctionConfigurationResponse
            {
                State = State.Pending,
                LastModified = "2015-12-11T12:28:30.45Z"
            };

            _packageTypeImageResponse = new GetFunctionConfigurationResponse()
            {
                State = State.Active,
                PackageType = PackageType.Image,
                ImageConfigResponse = new ImageConfigResponse()
                {
                    ImageConfig = new ImageConfig()
                    {
                        Command = new List<string>()
                        {
                            "command1",
                            "command2",
                            "command3",
                        },
                        EntryPoint = new List<string>()
                        {
                            "entry1",
                            "entry2",
                            "entry3",
                        },
                        WorkingDirectory = "/some/dir",
                    }
                },
                CodeSize = 0,
                LastModified = "2015-12-11T12:28:30.45Z",
            };
        }


        [Fact]
        public void ValidateInitialLoadProperties()
        {
            this._lambda.Setup(mock => mock.GetFunctionConfiguration(It.IsAny<String>()))
                .Returns(_getInitialResponse);
            _controller.RefreshFunctionConfiguration(this._lambda.Object);
            Assert.Equal(State.Pending.Value, _controller.Model.State);
            Assert.Null(this._controller.Model.LastUpdateStatus);
            Assert.False(_controller.Model.CanInvoke);
        }

        [Theory]
        [MemberData(nameof(ArchitectureData))]
        public void ValidateArchitecturesOnRefresh(List<string> architectureList, string expectedArchitectures)
        {
            var response = new GetFunctionConfigurationResponse()
            {
                Architectures = architectureList,
                LastModified = "2015-12-11T12:28:30.45Z"
            };
            _lambda.Setup(mock => mock.GetFunctionConfiguration(It.IsAny<String>()))
                .Returns(response);
            _controller.RefreshFunctionConfiguration(this._lambda.Object);

            Assert.Equal(expectedArchitectures, _controller.Model.Architectures);
        }

        [Fact]
        public void ValidatePendingStatePropertiesOnRefresh()
        {
            this._lambda.Setup(mock => mock.GetFunctionConfiguration(It.IsAny<String>()))
                .Returns(_getPendingResponse);
            _controller.RefreshFunctionConfiguration(this._lambda.Object);

            Assert.Equal(State.Pending.Value, _controller.Model.State);
            Assert.Equal(StateReasonCode.Creating.Value, _controller.Model.StateReasonCode);
            Assert.Equal("The function is being created", _controller.Model.StateReason);
            Assert.Equal(LastUpdateStatus.Successful.Value, _controller.Model.LastUpdateStatus);
            Assert.Null(_controller.Model.LastUpdateStatusReason);
            Assert.Null(_controller.Model.LastUpdateStatusReasonCode);
        }


        [Fact]
        public void ValidateProgressStatePropertiesOnUpdate()
        {
            this._lambda
                .Setup(mock => mock.UpdateFunctionConfiguration(It.IsAny<UpdateFunctionConfigurationRequest>()))
                .Returns(_updateProgressResponse);
            _controller.UpdateConfiguration(this._lambda.Object, new UpdateFunctionConfigurationRequest());

            Assert.Equal(State.Active.Value, _controller.Model.State);
            Assert.Null(_controller.Model.StateReasonCode);
            Assert.Null(_controller.Model.StateReason);
            Assert.Equal(LastUpdateStatus.InProgress.Value, _controller.Model.LastUpdateStatus);
            Assert.Equal("The function is being created", _controller.Model.LastUpdateStatusReason);
            Assert.Equal(LastUpdateStatusReasonCode.EniLimitExceeded.Value, _controller.Model.LastUpdateStatusReasonCode);
        }

        [Fact]
        public void ValidatePendingInvokePropertiesOnRefresh()
        {
            this._lambda.Setup(mock => mock.GetFunctionConfiguration(It.IsAny<String>()))
                .Returns(_getPendingResponse);
            _controller.RefreshFunctionConfiguration(this._lambda.Object);

            Assert.False(_controller.Model.CanInvoke);
            Assert.Equal(Visible, _controller.Model.InvokeWarningVisibility);
            Assert.Contains(_controller.Model.StateReason, _controller.Model.InvokeWarningText);
            Assert.Contains(_controller.Model.StateReason, _controller.Model.InvokeWarningTooltip);
        }

        [Fact]
        public void ValidateProgressInvokePropertiesOnUpdate()
        {
            this._lambda
                .Setup(mock => mock.UpdateFunctionConfiguration(It.IsAny<UpdateFunctionConfigurationRequest>()))
                .Returns(_updateProgressResponse);
            _controller.UpdateConfiguration(this._lambda.Object, new UpdateFunctionConfigurationRequest());

            Assert.True(_controller.Model.CanInvoke);
            Assert.Equal(Visible, _controller.Model.InvokeWarningVisibility);
            Assert.Contains(_controller.Model.LastUpdateStatus, _controller.Model.InvokeWarningText);
            Assert.Contains(_controller.Model.LastUpdateStatus, _controller.Model.InvokeWarningTooltip);
        }

        [Fact]
        public void ValidateActiveInvokePropertiesOnRefresh()
        {
            this._lambda.Setup(mock => mock.GetFunctionConfiguration(It.IsAny<String>()))
                .Returns(_getActiveResponse);
            _controller.RefreshFunctionConfiguration(this._lambda.Object);

            Assert.True(_controller.Model.CanInvoke);
            Assert.Equal(Collapsed, _controller.Model.InvokeWarningVisibility);
            Assert.True(string.IsNullOrEmpty(_controller.Model.InvokeWarningText));
            Assert.Contains("Invoke Function", _controller.Model.InvokeWarningTooltip);
        }

        [Fact]
        public void RefreshFunctionConfiguration_ImageType()
        {
            _lambda.Setup(mock => mock.GetFunctionConfiguration(It.IsAny<String>()))
                .Returns(_packageTypeImageResponse);

            _lambda.Setup(mock => mock.GetFunction(It.IsAny<string>()))
                .Returns(new GetFunctionResponse()
                {
                    Code = new FunctionCodeLocation()
                    {
                        ImageUri = SampleImageUri
                    }
                });

            _controller.RefreshFunctionConfiguration(this._lambda.Object);

            Assert.Equal(PackageType.Image, _controller.Model.PackageType);
            Assert.Equal(SampleImageUri, _controller.Model.ImageUri);
            Assert.Equal(ViewFunctionModel.CodeSizeNotApplicable, _controller.Model.CodeSizeFormatted);
            Assert.Equal("command1,command2,command3", _controller.Model.ImageCommand);
            Assert.Equal("entry1,entry2,entry3", _controller.Model.ImageEntrypoint);
            Assert.Equal(_packageTypeImageResponse.ImageConfigResponse.ImageConfig.WorkingDirectory, _controller.Model.ImageWorkingDirectory);
        }

        [Theory]
        [InlineData("aaa,bbb,ccc")]
        [InlineData("aaa,,bbb,ccc")]
        [InlineData("aaa, bbb ,ccc")]
        [InlineData(" aaa , bbb , ccc ")]
        public void SplitByComma(string text)
        {
            var expectedList = new List<string>() { "aaa", "bbb", "ccc" };

            Assert.Equal(expectedList, ViewFunctionController.SplitByComma(text));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void SplitByComma_WhenEmpty(string text)
        {
            Assert.Null(ViewFunctionController.SplitByComma(text));
        }

        [Fact]
        public void JoinByComma()
        {
            var list = new List<string>() { "aaa", "bbb", "ccc" };
            AssertJoinByComma(list, "aaa,bbb,ccc");

            list = new List<string>() { " aaa ", " bbb ", " ccc " };
            AssertJoinByComma(list, " aaa , bbb , ccc ");

            list = new List<string>() { " aaa ", "", " bbb ", " ccc " };
            AssertJoinByComma(list, " aaa ,, bbb , ccc ");
        }

        [Fact]
        public void GetLogStreamsView_InvalidViewer()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ILogStreamsViewer)))
                .Returns(null);

            var view = _controller.GetLogStreamsView();

            Assert.Null(view);
            ToolkitHost.Verify(
                mock => mock.ShowError(
                    It.Is<string>(msg => msg.Contains("Error viewing log group for lambda function"))),
                Times.Once);
        }

        [StaFact]
        public void GetLogStreamsView()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ILogStreamsViewer)))
                .Returns(_logStreamsViewer.Object);
            _logStreamsViewer.Setup(mock => mock.GetViewer(It.IsAny<string>(), It.IsAny<AwsConnectionSettings>()))
                .Returns(new BaseAWSControl());

            var view = _controller.GetLogStreamsView();

            Assert.IsType<BaseAWSControl>(view);
            _logStreamsViewer.Verify(
                mock => mock.GetViewer(It.IsAny<string>(), It.IsAny<AwsConnectionSettings>()), Times.Once);
        }

        public static IEnumerable<object[]> OpenLogGroupData = new List<object[]>
        {
            new object[] {true, Result.Succeeded},
            new object[] {false, Result.Failed}
        };

        [Theory]
        [MemberData(nameof(OpenLogGroupData))]
        public void RecordOpenLogGroup(bool result, Result expectedResult)
        {
            _controller.RecordOpenLogGroup(result);

            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(expectedResult,
                CloudWatchResourceType.LogGroup, MetricSources.LambdaMetricSource.LambdaView);
        }

        [Fact]
        public void RecordOpenLogGroup_OncePerSession()
        {
            _controller.RecordOpenLogGroup(true);
            _controller.RecordOpenLogGroup(false);

            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Succeeded,
             CloudWatchResourceType.LogGroup, MetricSources.LambdaMetricSource.LambdaView);
        }

        private void AssertJoinByComma(List<string> strings, string expectedText)
        {
            Assert.Equal(expectedText, ViewFunctionController.JoinByComma(strings));
        }
    }
}
