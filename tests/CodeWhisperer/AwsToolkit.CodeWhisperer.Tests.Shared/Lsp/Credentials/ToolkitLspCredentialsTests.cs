using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.Runtime;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Credentials
{
    public class ToolkitLspCredentialsTests
    {
        private readonly FakeLspCredentials _credentialsProtocol = new FakeLspCredentials();
        private readonly ToolkitLspCredentials _sut;

        public ToolkitLspCredentialsTests()
        {
            _sut = new ToolkitLspCredentials(new CredentialsEncryption(), _credentialsProtocol);
        }

        [Fact]
        public void DeleteCredentials()
        {
            _credentialsProtocol.CredentialsPayload = "some previous value";

            _sut.DeleteCredentials();

            _credentialsProtocol.CredentialsPayload.Should().BeNull();
        }

        [Fact]
        public void DeleteToken()
        {
            _credentialsProtocol.TokenPayload = "some previous value";

            _sut.DeleteToken();

            _credentialsProtocol.TokenPayload.Should().BeNull();
        }

        [Fact]
        public async Task UpdateCredentials()
        {
            await _sut.UpdateCredentialsAsync(new ImmutableCredentials("access", "secret", "token"));

            _credentialsProtocol.CredentialsPayload.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateToken()
        {
            await _sut.UpdateTokenAsync(new BearerToken() { Token = "token" });

            _credentialsProtocol.TokenPayload.Should().NotBeNull();
        }
    }
}
