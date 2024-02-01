using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.Runtime.Credentials.Internal;

using AwsToolkit.VsSdk.Common.CommonUI.Models;

using Microsoft.VisualStudio.Threading;

using Moq;

using Xunit;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace AwsToolkit.Vs.Tests.VsSdk.Common.CommonUI
{
    public class TestSsoLoginViewModel : SsoBuilderIdLoginViewModel
    {
        public async Task ExposeBeginLoginFlowAsync()
        {
            await base.BeginLoginFlowAsync();
        }

        public CancellationToken CancellationToken => _linkedCancellationToken;

        public TestSsoLoginViewModel(ISSOTokenManager ssoTokenManager,
            SSOTokenManagerGetTokenOptions tokenManagerOptions, ToolkitContext toolkitContext,
            JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken) : base(ssoTokenManager, tokenManagerOptions, toolkitContext,
            joinableTaskFactory, cancellationToken)
        {
        }
    }

    public class SsoLoginViewModelTests : IDisposable
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly TestSsoLoginViewModel _sut;
        private readonly SsoToken _sampleSsoToken = new SsoToken();
        private readonly Mock<ISSOTokenManager> _ssoTokenManager = new Mock<ISSOTokenManager>();
        private readonly SSOTokenManagerGetTokenOptions _options = new SSOTokenManagerGetTokenOptions();

        public SsoLoginViewModelTests()
        {
#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005
            _sut = new TestSsoLoginViewModel(_ssoTokenManager.Object, _options, _toolkitContextFixture.ToolkitContext,
                taskContext.Factory, default);
        }

        [Fact]
        public async Task BeginLoginFlow_WhenExceptionThrown()
        {
            Assert.Null(_sut.DialogResult);
            Assert.Equal(TaskStatus.Cancel, _sut.LoginResult.Status);

            SetupSsoTokenManager_Throws();
            await Assert.ThrowsAsync<Exception>(async () => await _sut.ExposeBeginLoginFlowAsync());

            Assert.Null(_sut.SsoToken);
            Assert.Equal(TaskStatus.Fail, _sut.LoginResult.Status);
            Assert.False(_sut.DialogResult);
        }


        [Fact]
        public async Task BeginLoginFlow_WhenSuccessful()
        {
            Assert.Null(_sut.DialogResult);
            Assert.Equal(TaskStatus.Cancel, _sut.LoginResult.Status);

            SetupSsoTokenManager_Success();
            await _sut.ExposeBeginLoginFlowAsync();

            Assert.Equal(_sampleSsoToken, _sut.SsoToken);
            Assert.Equal(TaskStatus.Success, _sut.LoginResult.Status);
            Assert.True(_sut.DialogResult);
        }

        [Fact]
        public async Task BeginLoginFlow_WhenDialogAlreadyClosed()
        {
            _sut.DialogResult = false;
            _sut.LoginResult.Status = TaskStatus.Cancel;

            SetupSsoTokenManager_Throws();
            var exception = await Record.ExceptionAsync(async () => await _sut.ExposeBeginLoginFlowAsync());

            Assert.Null(exception);
            Assert.Null(_sut.SsoToken);
            Assert.NotEqual(TaskStatus.Fail, _sut.LoginResult.Status);
        }

        [Fact]
        public async Task BeginLoginFlow_WhenSsoCallbackModified()
        {
            var ssoCallback = _options.SsoVerificationCallback;

            SetupSsoTokenManager_Success();
            await _sut.ExposeBeginLoginFlowAsync();

            Assert.NotEqual(ssoCallback, _options.SsoVerificationCallback);
            Assert.Equal(_sampleSsoToken, _sut.SsoToken);
            Assert.Equal(TaskStatus.Success, _sut.LoginResult.Status);
        }

        [Fact]
        public void VerifyCancelActionUpdatesState()
        {
            _sut.CancelDialogCommand.Execute(null);

            Assert.True(_sut.CancellationToken.IsCancellationRequested);
            Assert.Equal(TaskStatus.Cancel, _sut.LoginResult.Status);
            Assert.False(_sut.DialogResult);
        }

        private void SetupSsoTokenManager_Success()
        {
            _ssoTokenManager
                .Setup(mock =>
                    mock.GetTokenAsync(It.IsAny<SSOTokenManagerGetTokenOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_sampleSsoToken);
        }

        private void SetupSsoTokenManager_Throws()
        {
            _ssoTokenManager
                .Setup(mock =>
                    mock.GetTokenAsync(It.IsAny<SSOTokenManagerGetTokenOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception());
        }

        public void Dispose()
        {
            _sut?.Dispose();
        }
    }
}
