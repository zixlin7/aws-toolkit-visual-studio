using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Settings;
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
using Amazon.Runtime;

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
            ICodeWhispererSettingsRepository settingsRepository,
            ICodeWhispererLspClient codeWhispererLspClient,
            ICodeWhispererSsoTokenProvider ssoTokenProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider,
            IToolkitTimer timer)
            : base(
                toolkitContextProvider,
                settingsRepository,
                codeWhispererLspClient,
                ssoTokenProvider,
                taskFactoryProvider,
                timer)
        {
        }

        protected override Task<ICredentialIdentifier> PromptUserForCredentialIdAsync()
        {
            return Task.FromResult(CredentialIdPromptResponse);
        }
    }

    [Collection(VsMockCollection.CollectionName)]
    public class ConnectionTests : IDisposable
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;
        private readonly FakeCodeWhispererSettingsRepository _settingsRepository = new FakeCodeWhispererSettingsRepository();
        private readonly FakeCodeWhispererClient _codeWhispererClient = new FakeCodeWhispererClient();
        private readonly FakeCodeWhispererSsoTokenProvider _ssoTokenProvider = new FakeCodeWhispererSsoTokenProvider();
        private readonly FakeToolkitTimer _timer = new FakeToolkitTimer();

        private readonly ICredentialIdentifier _sampleCredentialId = FakeCredentialIdentifier.Create("AwsBuilderId");

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
            SsoRegistrationScopes = SonoProperties.CodeWhispererScopes,
            SsoStartUrl = "sample-start-url"
        };

        private readonly StubConnection _sut;

        public ConnectionTests(GlobalServiceProvider serviceProvider)
        {
            serviceProvider.Reset();

            _taskFactoryProvider = new ToolkitJoinableTaskFactoryProvider(ThreadHelper.JoinableTaskContext);

            _ssoTokenProvider.CanGetTokenSilently = false;
            _ssoTokenProvider.Token = new AWSToken()
            {
                Token = "secret-token-123", ExpiresAt = DateTime.UtcNow.AddHours(6),
            };

            _settingsRepository.Settings.CredentialIdentifier = _sampleCredentialId.Id;

            _toolkitContextFixture.DefineRegion(_sampleRegion);
            _toolkitContextFixture.DefineCredentialIdentifiers(new[] { _sampleCredentialId });
            _toolkitContextFixture.DefineCredentialProperties(_sampleCredentialId, _sampleProfileProperties);

            _sut = new StubConnection(
                _toolkitContextFixture.ToolkitContextProvider,
                _settingsRepository,
                _codeWhispererClient,
                _ssoTokenProvider,
                _taskFactoryProvider,
                _timer);
        }

        [Fact]
        public async Task SignInAsync_UserCancel()
        {
            _settingsRepository.Settings.CredentialIdentifier = null;
            _sut.CredentialIdPromptResponse = null;
            await _sut.SignInAsync();

            _sut.Status.Should().Be(ConnectionStatus.Disconnected);
            _timer.IsStarted.Should().BeFalse();
            _codeWhispererClient.CredentialsProtocol.TokenPayload.Should().BeNull();
            _settingsRepository.Settings.CredentialIdentifier.Should().BeNull();
        }

        [Fact]
        public async Task SignInAsync()
        {
            _settingsRepository.Settings.CredentialIdentifier = null;
            _sut.CredentialIdPromptResponse = _sampleCredentialId;
            await _sut.SignInAsync();

            _sut.Status.Should().Be(ConnectionStatus.Connected);
            _timer.IsStarted.Should().BeTrue();
            _codeWhispererClient.CredentialsProtocol.TokenPayload.Token.Should().Be(_ssoTokenProvider.Token.Token);
            _settingsRepository.Settings.CredentialIdentifier.Should().Be(_sampleCredentialId.Id);
        }

        [Fact]
        public async Task SignOutAsync()
        {
            // set up: Sign in with a token expiring in 6 hours
            const int tokenDurationHours = 6;
            _ssoTokenProvider.Token.ExpiresAt = DateTime.UtcNow.AddHours(tokenDurationHours);

            _sut.CredentialIdPromptResponse = _sampleCredentialId;
            await _sut.SignInAsync();

            // act
            await _sut.SignOutAsync();

            _sut.Status.Should().Be(ConnectionStatus.Disconnected);
            _timer.IsStarted.Should().BeFalse();
            _codeWhispererClient.CredentialsProtocol.TokenPayload.Should().BeNull();
            _settingsRepository.Settings.CredentialIdentifier.Should().BeNull();
        }


        [Fact]
        public async Task RequestConnectionMetadataAsync_WhenSignedIn()
        {
            _settingsRepository.Settings.CredentialIdentifier = null;
            _sut.CredentialIdPromptResponse = _sampleCredentialId;
            await _sut.SignInAsync();

            var eventArgs = new ConnectionMetadataEventArgs();
            await _codeWhispererClient.RaiseRequestConnectionMetadataAsync(eventArgs);

            eventArgs.Response.Sso.StartUrl.Should().BeEquivalentTo(_sampleProfileProperties.SsoStartUrl);
        }

        [Fact]
        public async Task RequestConnectionMetadataAsync_WhenSignedOut()
        {
            // set up: Sign in with a token expiring in 6 hours
            const int tokenDurationHours = 6;
            _ssoTokenProvider.Token.ExpiresAt = DateTime.UtcNow.AddHours(tokenDurationHours);

            _sut.CredentialIdPromptResponse = _sampleCredentialId;
            await _sut.SignInAsync();
            await _sut.SignOutAsync();

            var eventArgs = new ConnectionMetadataEventArgs();
            await _codeWhispererClient.RaiseRequestConnectionMetadataAsync(eventArgs);

            eventArgs.Response.Sso.StartUrl.Should().BeNull();
        }

        [Fact]
        public async Task LongLivedTokenRefreshesFiveMinutesBeforeExpiration()
        {
            // set up: Sign in will occur with a token expiring in 6 hours
            const int tokenDurationHours = 6;
            _ssoTokenProvider.Token.ExpiresAt = DateTime.UtcNow.AddHours(tokenDurationHours);

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
            _ssoTokenProvider.Token.ExpiresAt = DateTime.UtcNow.AddMinutes(tokenDurationMinutes);

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
            _ssoTokenProvider.Token.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenDurationSeconds);

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
            _ssoTokenProvider.Token.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

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

        [Fact]
        public async Task RefreshSetsUpNextRefresh()
        {
            _ssoTokenProvider.CanGetTokenSilently = true;

            // set up: Simulate signing in, but then the Token has expired
            _ssoTokenProvider.Token.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

            _sut.CredentialIdPromptResponse = _sampleCredentialId;
            await _sut.SignInAsync();

            // set up: The refreshed token expires well into the future
            const int tokenDurationHours = 6;
            _ssoTokenProvider.Token.ExpiresAt = DateTime.UtcNow.AddHours(tokenDurationHours);

            // Act: Invoke the Connection component's refresh mechanics. This should refresh, and set up the next refresh timer.
            _timer.RaiseElapsed();

            _timer.IsStarted.Should().BeTrue();

            // Should refresh "five minutes before expiration"
            var expectedMinInterval = new TimeSpan(0, tokenDurationHours - 1, 53, 0).TotalMilliseconds;
            var expectedMaxInterval = new TimeSpan(0, tokenDurationHours - 1, 55, 0).TotalMilliseconds;

            _timer.Interval.Should().BeInRange(expectedMinInterval, expectedMaxInterval);
        }

        [Fact]
        public async Task Refresh_LoginRequired()
        {
            _ssoTokenProvider.CanGetTokenSilently = false;

            // set up: Simulate signing in
            _sut.CredentialIdPromptResponse = _sampleCredentialId;
            await _sut.SignInAsync();

            // Act: Invoke the Connection component's refresh mechanics. This should fail to refresh, and then sign out
            _timer.RaiseElapsed();

            _timer.IsStarted.Should().BeFalse();
            _codeWhispererClient.CredentialsProtocol.TokenPayload.Should().BeNull();
        }

        [Fact]
        public async Task LspInit_NoPreviousSignIn()
        {
            _settingsRepository.Settings.CredentialIdentifier = null;

            await _codeWhispererClient.RaiseInitializedAsync();

            _codeWhispererClient.CredentialsProtocol.TokenPayload.Should().BeNull();
        }

        [Fact]
        public async Task LspInit_PreviousCredentialsIdNotFound()
        {
            _settingsRepository.Settings.CredentialIdentifier = "nonexistent-credentials-id";

            await _codeWhispererClient.RaiseInitializedAsync();

            _codeWhispererClient.CredentialsProtocol.TokenPayload.Should().BeNull();
        }

        [Fact]
        public async Task LspInit_PreviouslySignedIn()
        {
            _ssoTokenProvider.CanGetTokenSilently = true;
            await _codeWhispererClient.RaiseInitializedAsync();

            _codeWhispererClient.CredentialsProtocol.TokenPayload.Token.Should().Be(_ssoTokenProvider.Token.Token);
        }

        [Fact]
        public async Task LspInit_CannotRefreshPreviousToken()
        {
            _ssoTokenProvider.CanGetTokenSilently = false;
            await _codeWhispererClient.RaiseInitializedAsync();

            _codeWhispererClient.CredentialsProtocol.TokenPayload.Should().BeNull();
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _sut?.Dispose();
            _taskFactoryProvider?.Dispose();
        }
    }
}
