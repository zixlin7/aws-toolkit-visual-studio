using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Tests.TestUtilities;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.Time;
using Amazon.AWSToolkit.Util;

using FluentAssertions;

using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Credentials
{
    internal class StubConnection : Connection
    {
        public ICredentialIdentifier CredentialIdPromptResponse;

        public StubConnection(
            IToolkitContextProvider toolkitContextProvider,
            ICodeWhispererLspClient codeWhispererLspClient,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider,
            IToolkitTimer timer)
            : base(toolkitContextProvider, codeWhispererLspClient, taskFactoryProvider, timer)
        {
        }

        protected override ICredentialIdentifier PromptUserForCredentialId()
        {
            return CredentialIdPromptResponse;
        }
    }

    [Collection(VsMockCollection.CollectionName)]
    public class ConnectionTests : IDisposable
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;
        private readonly FakeCodeWhispererClient _codeWhispererClient = new FakeCodeWhispererClient();
        private readonly FakeToolkitTimer _timer = new FakeToolkitTimer();

        private readonly ICredentialIdentifier _sampleCredentialId = FakeCredentialIdentifier.Create("AwsBuilderId");
        private readonly FakeTokenProvider _tokenProvider = new FakeTokenProvider("secret-token-123");

        private readonly ToolkitRegion _sampleRegion = new ToolkitRegion()
        {
            Id = "sample-region",
            DisplayName = "sample-region",
            PartitionId = "aws",
        };

        private readonly ProfileProperties _sampleProfileProperties = new ProfileProperties()
        {
            SsoRegion = "sample-region",
            SsoSession = "aws-builder-id",
            SsoRegistrationScopes = SonoProperties.CodeWhispererScopes
        };

        private readonly StubConnection _sut;

        public ConnectionTests(GlobalServiceProvider serviceProvider)
        {
            serviceProvider.Reset();

            _taskFactoryProvider = new ToolkitJoinableTaskFactoryProvider(ThreadHelper.JoinableTaskContext);

            var toolkitCredentials = new ToolkitCredentials(_sampleCredentialId, _tokenProvider);
            _toolkitContextFixture.SetupGetToolkitCredentials(toolkitCredentials);

            _toolkitContextFixture.DefineRegion(_sampleRegion);
            _toolkitContextFixture.DefineCredentialIdentifiers(new[] { _sampleCredentialId });
            _toolkitContextFixture.DefineCredentialProperties(_sampleCredentialId, _sampleProfileProperties);

            _sut = new StubConnection(_toolkitContextFixture.ToolkitContextProvider, _codeWhispererClient, _taskFactoryProvider, _timer);
        }

        [Fact]
        public async Task SignInAsync_UserCancel()
        {
            _sut.CredentialIdPromptResponse = null;
            await _sut.SignInAsync();

            _codeWhispererClient.CredentialsProtocol.TokenPayload.Should().BeNull();
        }

        [Fact]
        public async Task SignInAsync()
        {
            _sut.CredentialIdPromptResponse = _sampleCredentialId;
            await _sut.SignInAsync();

            _codeWhispererClient.CredentialsProtocol.TokenPayload.Token.Should().Be(_tokenProvider.Token);
        }

        [Fact]
        public async Task SignOutAsync()
        {
            // set up: Sign in with a token expiring in 6 hours
            const int tokenDurationHours = 6;
            _tokenProvider.ExpiresAt = DateTime.UtcNow.AddHours(tokenDurationHours);

            _sut.CredentialIdPromptResponse = _sampleCredentialId;
            await _sut.SignInAsync();

            // act
            await _sut.SignOutAsync();

            _sut.Status.Should().Be(ConnectionStatus.Disconnected);
            _timer.IsStarted.Should().BeFalse();
            _codeWhispererClient.CredentialsProtocol.TokenPayload.Should().BeNull();
        }

        [Fact]
        public async Task LongLivedTokenRefreshesFiveMinutesBeforeExpiration()
        {
            // set up: Sign in will occur with a token expiring in 6 hours
            const int tokenDurationHours = 6;
            _tokenProvider.ExpiresAt = DateTime.UtcNow.AddHours(tokenDurationHours);

            _sut.CredentialIdPromptResponse = _sampleCredentialId;

            // act
            await _sut.SignInAsync();

            _timer.IsStarted.Should().BeTrue();

            // Should refresh "five minutes before expiration"
            // We test with a generous range in order to avoid false positives
            var expectedMinInterval = new TimeSpan(0, tokenDurationHours - 1, 53, 0).TotalMilliseconds;
            var expectedMaxInterval = new TimeSpan(0, tokenDurationHours - 1, 55, 0).TotalMilliseconds;

            _timer.Interval.Should().BeInRange(expectedMinInterval, expectedMaxInterval);
        }

        [Fact]
        public async Task ShortLivedTokenRefreshesInOneMinute()
        {
            // set up: Sign in will occur with a token expiring in 4 minutes (eg: sooner than the system's 5 minute buffer)
            const int tokenDurationMinutes = 4;
            _tokenProvider.ExpiresAt = DateTime.UtcNow.AddMinutes(tokenDurationMinutes);

            _sut.CredentialIdPromptResponse = _sampleCredentialId;

            // act
            await _sut.SignInAsync();

            _timer.IsStarted.Should().BeTrue();

            // Should refresh "a minute from now"
            // We test with a generous range in order to avoid false positives
            var expectedMinInterval = new TimeSpan(0, 0, 0, 55).TotalMilliseconds;
            var expectedMaxInterval = new TimeSpan(0, 0, 1, 0).TotalMilliseconds;

            _timer.Interval.Should().BeInRange(expectedMinInterval, expectedMaxInterval);
        }

        [Fact]
        public async Task ImminentTokenExpirationRefreshesOnExpiration()
        {
            // set up: Sign in will occur with a token expiring extremely soon (sooner than the system's smallest refresh window of 1 minute)
            const int tokenDurationSeconds = 20;
            _tokenProvider.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenDurationSeconds);

            _sut.CredentialIdPromptResponse = _sampleCredentialId;

            // act
            await _sut.SignInAsync();

            _timer.IsStarted.Should().BeTrue();

            // Should refresh "when the token expires"
            // We test with a generous range in order to avoid false positives
            var expectedMinInterval = new TimeSpan(0, 0, 0, tokenDurationSeconds - 4).TotalMilliseconds;
            var expectedMaxInterval = new TimeSpan(0, 0, 0, tokenDurationSeconds + 1).TotalMilliseconds;

            _timer.Interval.Should().BeInRange(expectedMinInterval, expectedMaxInterval);
        }

        [Fact]
        public async Task ExpiredTokenRefreshesImmediately()
        {
            // set up: Sign in will occur with a token that has expired (this is more to exercise the handling logic than sign in)
            _tokenProvider.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

            _sut.CredentialIdPromptResponse = _sampleCredentialId;

            // act
            await _sut.SignInAsync();

            _timer.IsStarted.Should().BeTrue();

            // Should refresh "three seconds from now"
            // We test with a generous range in order to avoid false positives
            var expectedMinInterval = new TimeSpan(0, 0, 0, 1).TotalMilliseconds;
            var expectedMaxInterval = new TimeSpan(0, 0, 0, 3).TotalMilliseconds;

            _timer.Interval.Should().BeInRange(expectedMinInterval, expectedMaxInterval);
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _sut?.Dispose();
            _taskFactoryProvider?.Dispose();
        }
    }
}
