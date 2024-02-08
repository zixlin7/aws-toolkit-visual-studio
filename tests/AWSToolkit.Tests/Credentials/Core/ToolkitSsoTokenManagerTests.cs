using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.Runtime;
using Amazon.Runtime.Credentials.Internal;

using Moq;

using Xunit;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class ToolkitSsoTokenManagerTests
    {
        private static readonly Action<SsoVerificationArguments> _ssoCallbackStub = _ => { };
        private static readonly SsoToken _sampleSsoToken = new SsoToken();
        private readonly Mock<ISSOTokenManager> _ssoTokenManager = new Mock<ISSOTokenManager>();
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly Mock<ISsoLoginDialog> _dialog = new Mock<ISsoLoginDialog>();
        private readonly Mock<ISsoLoginDialogFactory> _ssoDialogFactory = new Mock<ISsoLoginDialogFactory>();

        private ToolkitSsoTokenManagerOptions _toolkitOptions;
        private readonly ToolkitSsoTokenManager _sut;
        private readonly SSOTokenManagerGetTokenOptions _options = new SSOTokenManagerGetTokenOptions();


        public ToolkitSsoTokenManagerTests()
        {
            SetupSsoTokenManager(_sampleSsoToken);
            SetupLoginDialog();

            _toolkitOptions = new ToolkitSsoTokenManagerOptions("client-name", "client-type", "credential-name", _ssoCallbackStub,
                new[] { "scope" }, false);

            _sut = new ToolkitSsoTokenManager(_ssoTokenManager.Object, _toolkitOptions, _toolkitContextFixture.ToolkitHost.Object);
        }

        [Fact]
        public void GetToken_WhenCallbackOverride()
        {
            var token = _sut.GetToken(_options);

            Assert.Equal(_sampleSsoToken, token);
            _ssoTokenManager.Verify(mock => mock.GetToken(ItIsAdjustedByToolkitSsoTokenManager()), Times.Once);
        }

        [Fact]
        public void GetToken_WhenLoginRequired()
        {

            SetupSsoTokenManager(null);
            _toolkitOptions = new ToolkitSsoTokenManagerOptions("client-name", "client-type", "credential-name", null,
                new[] { "scope" }, true);
            var sut = new ToolkitSsoTokenManager(_ssoTokenManager.Object, _toolkitOptions,
                _toolkitContextFixture.ToolkitHost.Object);

            sut.GetToken(_options);

            _ssoDialogFactory.Verify(x => x.CreateSsoTokenProviderLoginDialog(It.IsAny<ISSOTokenManager>(), It.IsAny<SSOTokenManagerGetTokenOptions>(), true), Times.Once);

        }

        [Fact]
        public async Task GetTokenAsync_WhenCallbackOverride()
        {
            var token = await _sut.GetTokenAsync(_options);

            Assert.Equal(_sampleSsoToken, token);
            _ssoTokenManager.Verify(
                mock => mock.GetTokenAsync(ItIsAdjustedByToolkitSsoTokenManager(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetTokenAsync_WhenLoginRequired()
        {
            SetupSsoTokenManager(null);
            _toolkitOptions = new ToolkitSsoTokenManagerOptions("client-name", "client-type", "credential-name", null,
                new[] { "scope" }, true);

           
            var sut = new ToolkitSsoTokenManager(_ssoTokenManager.Object, _toolkitOptions,
                _toolkitContextFixture.ToolkitHost.Object);

            await sut.GetTokenAsync(_options);

            _ssoDialogFactory.Verify(x => x.CreateSsoTokenProviderLoginDialog(It.IsAny<ISSOTokenManager>(), It.IsAny<SSOTokenManagerGetTokenOptions>(), It.IsAny<bool>()), Times.Once);

        }

        private void SetupSsoTokenManager(SsoToken token)
        {
            _ssoTokenManager.Setup(mock => mock.GetToken(It.IsAny<SSOTokenManagerGetTokenOptions>()))
                .Returns(token);
            _ssoTokenManager
                .Setup(mock =>
                    mock.GetTokenAsync(It.IsAny<SSOTokenManagerGetTokenOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);
        }

        private void SetupLoginDialog()
        {
            _toolkitContextFixture.SetupExecuteOnUIThread();

            _dialog.Setup(x => x.DoLoginFlow()).Returns(new TaskResult() { Status = TaskStatus.Success });
            _ssoDialogFactory.Setup(x => x.CreateSsoTokenProviderLoginDialog(It.IsAny<ISSOTokenManager>(),
                It.IsAny<SSOTokenManagerGetTokenOptions>(), It.IsAny<bool>())).Returns(_dialog.Object);
            _toolkitContextFixture.ToolkitHost.Setup(x =>
                    x.GetDialogFactory().CreateSsoLoginDialogFactory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(_ssoDialogFactory.Object);
        }

        private SSOTokenManagerGetTokenOptions ItIsAdjustedByToolkitSsoTokenManager()
        {
            return It.Is<SSOTokenManagerGetTokenOptions>(options =>
                options.SsoVerificationCallback == _toolkitOptions.SsoVerificationCallback
                && options.SupportsGettingNewToken == true
                && options.ClientName == _toolkitOptions.ClientName
                && options.ClientType == _toolkitOptions.ClientType
                && _options.Scopes.All(s => _toolkitOptions.Scopes.Contains(s)));
        }
    }
}
