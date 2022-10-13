using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.Runtime;
using Amazon.Runtime.Credentials.Internal;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class ToolkitSsoTokenManagerTests
    {
        private static readonly Action<SsoVerificationArguments> SsoCallbackStub = _ => { };
        private static readonly SsoToken SampleSsoToken = new SsoToken();
        private readonly Mock<ISSOTokenManager> _ssoTokenManager = new Mock<ISSOTokenManager>();
        private readonly ToolkitSsoTokenManagerOptions _options;
        private readonly ToolkitSsoTokenManager _sut;

        public ToolkitSsoTokenManagerTests()
        {
            SetupSsoTokenManager();

            _options = new ToolkitSsoTokenManagerOptions("client-name", "client-type", SsoCallbackStub,
                new[] { "scope" });

            _sut = new ToolkitSsoTokenManager(_ssoTokenManager.Object, _options);
        }

        private void SetupSsoTokenManager()
        {
            _ssoTokenManager.Setup(mock => mock.GetToken(It.IsAny<SSOTokenManagerGetTokenOptions>()))
                .Returns(SampleSsoToken);
            _ssoTokenManager
                .Setup(mock =>
                    mock.GetTokenAsync(It.IsAny<SSOTokenManagerGetTokenOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleSsoToken);
        }

        [Fact]
        public void GetToken()
        {
            SSOTokenManagerGetTokenOptions options = new SSOTokenManagerGetTokenOptions();

            var token = _sut.GetToken(options);

            Assert.Equal(SampleSsoToken, token);
            _ssoTokenManager.Verify(mock => mock.GetToken(ItIsAdjustedByToolkitSsoTokenManager()), Times.Once);
        }

        [Fact]
        public async Task GetTokenAsync()
        {
            SSOTokenManagerGetTokenOptions options = new SSOTokenManagerGetTokenOptions();

            var token = await _sut.GetTokenAsync(options);

            Assert.Equal(SampleSsoToken, token);
            _ssoTokenManager.Verify(
                mock => mock.GetTokenAsync(ItIsAdjustedByToolkitSsoTokenManager(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        public SSOTokenManagerGetTokenOptions ItIsAdjustedByToolkitSsoTokenManager() =>
            It.Is<SSOTokenManagerGetTokenOptions>(options =>
                options.SsoVerificationCallback == _options.SsoVerificationCallback
                && options.SupportsGettingNewToken == true
                && options.ClientName == _options.ClientName
                && options.ClientType == _options.ClientType
                && _options.Scopes.All(s => _options.Scopes.Contains(s)));
    }
}
