using System.Collections.Generic;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.VisualStudio.GettingStarted;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.GettingStarted
{
    public class GettingStartedViewModelTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private List<ICredentialIdentifier> _credentialIdentifiers = new List<ICredentialIdentifier>();

        public GettingStartedViewModelTests()
        {
            _toolkitContextFixture.CredentialManager.Setup(mock => mock.GetCredentialIdentifiers())
                .Returns(() => _credentialIdentifiers);

            _toolkitContextFixture.CredentialSettingsManager.Setup(mock => mock.GetProfileProperties(It.IsAny<ICredentialIdentifier>()))
                .Returns(() => new ProfileProperties());
        }

        [Fact]
        public void ConnectionStateNonTerminalSetsStatusNull()
        {
            var cnMgrMock = new Mock<IAwsConnectionManager>();
            var sut = new GettingStartedViewModel(_toolkitContextFixture.ToolkitContext, cnMgrMock.Object)
            {
                Status = true // Change to ensure expected behavior as default is null
            };

            var e = new ConnectionStateChangeArgs() { State = new ConnectionState.InitializingToolkit() };
            cnMgrMock.Raise(mock => mock.ConnectionStateChanged += null, e);

            Assert.Null(sut.Status);
        }

        [Fact]
        public void ConnectionStateIsValidSetsStatusTrue()
        {
            var cnMgrMock = new Mock<IAwsConnectionManager>();
            var sut = new GettingStartedViewModel(_toolkitContextFixture.ToolkitContext, cnMgrMock.Object);

            var id = new FakeCredentialIdentifier() { DisplayName = "nobody" };
            var region = new ToolkitRegion() { DisplayName = "nowhere-east-6" };
            var e = new ConnectionStateChangeArgs() { State = new ConnectionState.ValidConnection(id, region) };
            cnMgrMock.Raise(mock => mock.ConnectionStateChanged += null, e);

            Assert.True(sut.Status);
        }

        [Fact]
        public void ConnectionStateIsInvalidSetsStatusFalse()
        {
            var cnMgrMock = new Mock<IAwsConnectionManager>();
            var sut = new GettingStartedViewModel(_toolkitContextFixture.ToolkitContext, cnMgrMock.Object);

            var e = new ConnectionStateChangeArgs() { State = new ConnectionState.InvalidConnection("kaboom") };
            cnMgrMock.Raise(mock => mock.ConnectionStateChanged += null, e);

            Assert.False(sut.Status);
        }

        [Fact]
        public void NoCredentialsDefinedSetsActiveCardToAddProfileCard()
        {
            _credentialIdentifiers.Clear();
            var sut = new GettingStartedViewModel(_toolkitContextFixture.ToolkitContext);

            Assert.Equal(GettingStartedViewModel.AddProfileCardName, sut.ActiveCard);
        }

        [Fact]
        public void AnySharedOrSdkCredentialsDefinedSetsActiveCardToGettingStartedCard()
        {
            _credentialIdentifiers.Add(new FakeCredentialIdentifier() {
                ProfileName = "default",
                FactoryId = SharedCredentialProviderFactory.SharedProfileFactoryId
            });
            var sut = new GettingStartedViewModel(_toolkitContextFixture.ToolkitContext);

            Assert.Equal(GettingStartedViewModel.GettingStartedCardName, sut.ActiveCard);
        }
    }
}
