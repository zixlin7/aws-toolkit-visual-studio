using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Credentials
{
    public class CredentialSelectionDialogViewModelTests
    {
        private readonly CredentialSelectionDialogViewModel _sut;

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private readonly ICredentialIdentifier[] _credIds = new[]
        {
            FakeCredentialIdentifier.Create("AwsBuilderId"),
            FakeCredentialIdentifier.Create("IdcRole"),
            FakeCredentialIdentifier.Create("Idc"),
            FakeCredentialIdentifier.Create("IdcOneCowScope"),
            FakeCredentialIdentifier.Create("IdcNoScopes"),
            FakeCredentialIdentifier.Create("IamCredentials")
        };

        private readonly ProfileProperties[] _profiles = new[]
        {
            new ProfileProperties() { SsoSession = "aws-builder-id", SsoRegistrationScopes = SonoProperties.CodeWhispererScopes },
            new ProfileProperties() { SsoSession = "idc-role", SsoAccountId = "012345678901", SsoRoleName = "role" },
            new ProfileProperties() { SsoSession = "idc", SsoRegistrationScopes = SonoProperties.CodeWhispererScopes },
            new ProfileProperties() { SsoSession = "idc-one-cow-scope", SsoRegistrationScopes = new[] { SonoProperties.CodeWhispererCompletionsScope } },
            new ProfileProperties() { SsoSession = "idc-no-scopes" },
            new ProfileProperties() { AccessKey = "access-key" }
        };

        public CredentialSelectionDialogViewModelTests()
        {
            _toolkitContextFixture.DefineCredentialIdentifiers(_credIds);
            for (var i = 0; i < _credIds.Length; i++)
            {
                _toolkitContextFixture.DefineCredentialProperties(_credIds[i], _profiles[i]);
            }

            _sut = new CredentialSelectionDialogViewModel(_toolkitContextFixture.ToolkitContextProvider);
        }

        [Fact]
        public void CredentialIdentifiersFilterToAwsBuilderIdAndIdcWithScopes()
        {
            Assert.Equal(2, _sut.CredentialIdentifiers.Count);
            Assert.Contains(_credIds[0], _sut.CredentialIdentifiers);
            Assert.Contains(_credIds[2], _sut.CredentialIdentifiers);
        }
    }
}
