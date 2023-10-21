using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Credentials
{
    internal class StubConnection : Connection
    {
        public ICredentialIdentifier CredentialIdPromptResponse;

        public StubConnection(IToolkitContextProvider toolkitContextProvider, ICodeWhispererLspClient codeWhispererLspClient)
            : base(toolkitContextProvider, codeWhispererLspClient)
        {
        }

        protected override ICredentialIdentifier PromptUserForCredentialId()
        {
            return CredentialIdPromptResponse;
        }
    }

    public class ConnectionTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly FakeCodeWhispererClient _codeWhispererClient = new FakeCodeWhispererClient();

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

        public ConnectionTests()
        {
            var toolkitCredentials = new ToolkitCredentials(_sampleCredentialId, _tokenProvider);
            _toolkitContextFixture.SetupGetToolkitCredentials(toolkitCredentials);

            _toolkitContextFixture.DefineRegion(_sampleRegion);
            _toolkitContextFixture.DefineCredentialIdentifiers(new[] { _sampleCredentialId });
            _toolkitContextFixture.DefineCredentialProperties(_sampleCredentialId, _sampleProfileProperties);

            _sut = new StubConnection(_toolkitContextFixture.ToolkitContextProvider, _codeWhispererClient);
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
    }
}
