using System;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Castle.Core.Internal;
using Moq;
using Xunit;
using static System.Windows.Visibility;

namespace AWSToolkit.Tests.Lambda
{
    public class ViewFunctionControllerTests
    {
        private readonly Mock<IAmazonLambda> _lambda = new Mock<IAmazonLambda>();
        private readonly ViewFunctionController _controller;
        private readonly GetFunctionConfigurationResponse _getPendingResponse;
        private readonly GetFunctionConfigurationResponse _getActiveResponse;
        private readonly GetFunctionConfigurationResponse _getInitialResponse;
        private readonly UpdateFunctionConfigurationResponse _updateProgressResponse;

        public ViewFunctionControllerTests()
        {
            _controller = new ViewFunctionController("mockFunctionName", "mockFunctionArn");
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

        [Fact]
        public void ValidatePendingStatePropertiesOnRefresh()
        {
            this._lambda.Setup(mock => mock.GetFunctionConfiguration(It.IsAny<String>()))
                .Returns(_getPendingResponse);
            _controller.RefreshFunctionConfiguration(this._lambda.Object);

            Assert.Equal(State.Pending.Value, _controller.Model.State);
            Assert.Equal(StateReasonCode.Creating.Value, _controller.Model.StateReasonCode);
            Assert.Equal("The function is being created",_controller.Model.StateReason);
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
            Assert.Equal( Visible, _controller.Model.InvokeWarningVisibility);
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
            Assert.True(_controller.Model.InvokeWarningText.IsNullOrEmpty());
            Assert.Contains("Invoke Function", _controller.Model.InvokeWarningTooltip);
        }
    }
}