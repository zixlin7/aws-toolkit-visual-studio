using System.Windows;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Sono
{
    public class SonoHelpersTests
    {
        private static readonly ICredentialIdentifier SonoCredentialId = new SonoCredentialIdentifier("default");

        private readonly Mock<IAWSToolkitShellProvider> _toolkitShell = new Mock<IAWSToolkitShellProvider>();

        [Fact]
        public void CreateSonoTokenManagerOptions()
        {
            void DoNothingCallback(SsoVerificationArguments verificationArgs) { }

            var options = SonoHelpers.CreateSonoTokenManagerOptions(DoNothingCallback);

            Assert.Contains("AwsToolkitForVisualStudio", options.ClientName);
            Assert.Contains("public", options.ClientType);
            Assert.Contains("sso:account:access", options.Scopes);
            Assert.Equal(DoNothingCallback, options.SsoVerificationCallback);
        }

        [Fact]
        public void CreateSsoCallbackShouldThrowOnCancel()
        {
            MockShellConfirmToFail();
            var ssoCallback = SonoHelpers.CreateSsoCallback(SonoCredentialId, _toolkitShell.Object);
            var callbackArgs = new SsoVerificationArguments();

            Assert.Throws<UserCanceledException>(() => ssoCallback(callbackArgs));
        }

        private void MockShellConfirmToFail()
        {
            _toolkitShell.Setup(mock =>
                    mock.Confirm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>()))
                .Returns(false);
        }
    }
}
