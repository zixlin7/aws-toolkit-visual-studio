using System;
using System.Windows;

using Amazon.AWSToolkit.CommonUI.Dialogs;
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

            Assert.Contains("AWS Toolkit for Visual Studio", options.ClientName);
            Assert.Contains("public", options.ClientType);
            Assert.Contains("sso:account:access", options.Scopes);
            Assert.Equal(DoNothingCallback, options.SsoVerificationCallback);
        }

        [Fact]

        public void CreateSsoCallbackShouldThrowOnCancel()
        {
            SetupDialogForCancellation();
            var ssoCallback = SonoHelpers.CreateSsoCallback(SonoCredentialId, _toolkitShell.Object);
            var callbackArgs = new SsoVerificationArguments();

            Assert.Throws<UserCanceledException>(() => ssoCallback(callbackArgs));
        }

        private void SetupDialogForCancellation()
        {
            var ssoDialog = new Mock<ISsoLoginDialog>();
            ssoDialog.Setup(mock => mock.Show()).Returns(false);

            _toolkitShell.Setup(mock => mock.ExecuteOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(action => action());
            _toolkitShell.Setup(mock => mock.GetDialogFactory().CreateSsoLoginDialog()).Returns(ssoDialog.Object);
        }
    }
}
