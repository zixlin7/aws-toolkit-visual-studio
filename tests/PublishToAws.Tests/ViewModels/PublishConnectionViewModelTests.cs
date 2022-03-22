using System.Collections.Generic;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using Microsoft.VisualStudio.Threading;

using Moq;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.ViewModels
{
    public class PublishConnectionViewModelTests
    {
        private static readonly ToolkitRegion SampleRegion = new ToolkitRegion()
        {
            DisplayName = "sample-region", Id = "sample-region",
        };

        private static readonly ICredentialIdentifier SampleCredentialId =
            FakeCredentialIdentifier.Create("sample-profile");

        private readonly PublishConnectionViewModel _sut;
        private readonly Mock<IAwsConnectionManager> _connectionManager = new Mock<IAwsConnectionManager>();

        public PublishConnectionViewModelTests()
        {
#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskCollection = new JoinableTaskContext();
#pragma warning restore VSSDK005
            var taskFactory = taskCollection.Factory;

            _sut = new PublishConnectionViewModel(_connectionManager.Object, taskFactory);
        }

        [Fact]
        public void StartListening_HandlesConnectionSettingsChanged()
        {
            _sut.StartListeningToConnectionManager();
            _connectionManager.Raise(mock => mock.ConnectionSettingsChanged += null,
                new ConnectionSettingsChangeArgs()
                {
                    Region = SampleRegion, CredentialIdentifier = SampleCredentialId,
                });

            Assert.Equal(SampleCredentialId, _sut.CredentialsId);
            Assert.Equal(SampleRegion, _sut.Region);
        }

        public static readonly IEnumerable<object[]> SampleConnectionStates = new[]
        {
            new object[]
            {
                new ConnectionState.ValidConnection(SampleCredentialId, SampleRegion), ConnectionStatus.Ok
            },
            new object[] { new ConnectionState.InvalidConnection("credentials are no good"), ConnectionStatus.Bad },
            new object[] { new ConnectionState.ValidatingConnection(), ConnectionStatus.Validating },
        };

        [Theory]
        [MemberData(nameof(SampleConnectionStates))]
        public void StartListening_HandlesConnectionStateChanged(
            ConnectionState connectionState,
            ConnectionStatus expectedStatus)
        {
            _sut.StartListeningToConnectionManager();
            _connectionManager.Raise(mock => mock.ConnectionStateChanged += null,
                new ConnectionStateChangeArgs() { State = connectionState, });

            Assert.Equal(expectedStatus, _sut.ConnectionStatus);
            Assert.Equal(connectionState.Message, _sut.StatusMessage);
        }
    }
}
