using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tests.Common.Context;

using AWSToolkit.Tests.Credentials.Core;
using Moq;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Utils
{
    public class ProfilePropertiesExtensionMethodsTests
    {
        [Fact]
        public void GetCredentialType_null()
        {
            ProfileProperties properties = null;
            Assert.Equal(CredentialType.Undefined, properties.GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_AssumeRoleMfa()
        {
            Assert.Equal(CredentialType.AssumeMfaRoleProfile,
                CredentialProfileTestHelper.Mfa.Valid.MfaReference.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_AssumeRoleExternalMfa()
        {
            Assert.Equal(CredentialType.AssumeMfaRoleProfile,
                CredentialProfileTestHelper.Mfa.Valid.ExternalSession.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_AssumeRole_SourceProfile()
        {
            Assert.Equal(CredentialType.AssumeRoleProfile,
                CredentialProfileTestHelper.AssumeRole.Valid.SourceProfile.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_AssumeRole_CredentialSource()
        {
            Assert.Equal(CredentialType.AssumeEc2InstanceRoleProfile,
                CredentialProfileTestHelper.AssumeRole.Valid.CredentialSource.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_SessionToken()
        {
            Assert.Equal(CredentialType.StaticSessionProfile,
                CredentialProfileTestHelper.Basic.Valid.Token.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_Static()
        {
            Assert.Equal(CredentialType.StaticProfile,
                CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_CredentialProcess()
        {
            Assert.Equal(CredentialType.CredentialProcessProfile,
                CredentialProfileTestHelper.CredentialProcess.ValidProfile.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_Saml()
        {
            Assert.Equal(CredentialType.AssumeSamlRoleProfile,
                CredentialProfileTestHelper.Saml.ValidProfile.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_Sso()
        {
            Assert.Equal(CredentialType.SsoProfile,
                CredentialProfileTestHelper.Sso.ValidProfile.AsProfileProperties().GetCredentialType());

            var profileProperties = new ProfileProperties {SsoAccountId = "sso-account"};
            Assert.Equal(CredentialType.SsoProfile, profileProperties.GetCredentialType());

            profileProperties = new ProfileProperties {SsoRegion = "sso-region"};
            Assert.Equal(CredentialType.SsoProfile, profileProperties.GetCredentialType());

            profileProperties = new ProfileProperties {SsoRoleName = "sso-role"};
            Assert.Equal(CredentialType.SsoProfile, profileProperties.GetCredentialType());

            profileProperties = new ProfileProperties {SsoStartUrl = "sso-url"};
            Assert.Equal(CredentialType.SsoProfile, profileProperties.GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_SsoBasedSsoSession()
        {
            Assert.Equal(CredentialType.SsoProfile,
                CredentialProfileTestHelper.SsoSession.Valid.ProfileReferencesSsoBasedSsoSession.AsProfileProperties()
                    .GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_TokenBasedSsoSession()
        {
            Assert.Equal(CredentialType.BearerToken,
                CredentialProfileTestHelper.SsoSession.Valid.ProfileReferencesTokenBasedSsoSession.AsProfileProperties()
                    .GetCredentialType());
        }

        [Fact]
        public async Task ValidateConnectionAsyncReturnsTrueOnValidCredentials()
        {
            var connectionManagerMock = new Mock<IAwsConnectionManager>();
            connectionManagerMock.SetupGet(mock => mock.IdentityResolver).Returns(new FakeIdentityResolver());

            var toolkitContext = new ToolkitContextFixture().ToolkitContext;
            toolkitContext.ConnectionManager = connectionManagerMock.Object;

            var sut = CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.AsProfileProperties();
            sut.Region = "region1-aws";

            Assert.True(await sut.ValidateConnectionAsync(toolkitContext));
        }

        [Fact]
        public async Task ValidateConnectionAsyncReturnsFalseOnInvalidCredentials()
        {
            var identityResolver = new FakeIdentityResolver
            {
                GetAccountIdAsyncThrows = true
            };

            var connectionManagerMock = new Mock<IAwsConnectionManager>();
            connectionManagerMock.SetupGet(mock => mock.IdentityResolver).Returns(identityResolver);

            var toolkitContext = new ToolkitContextFixture().ToolkitContext;
            toolkitContext.ConnectionManager = connectionManagerMock.Object;

            // Still needs to be a valid profile, just credentials that won't authenticate
            var sut = CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.AsProfileProperties();
            sut.Region = "region1-aws";

            Assert.False(await sut.ValidateConnectionAsync(toolkitContext));
        }

        [Fact]
        public async Task ValidateConnectionAsyncThrowsOnInvalidProfile()
        {
            var connectionManagerMock = new Mock<IAwsConnectionManager>();
            connectionManagerMock.SetupGet(mock => mock.IdentityResolver).Returns(new FakeIdentityResolver());

            var toolkitContext = new ToolkitContextFixture().ToolkitContext;
            toolkitContext.ConnectionManager = connectionManagerMock.Object;

            // This method validates the connection, not the profile.  That is expected to have happened already.
            var sut = CredentialProfileTestHelper.Basic.Invalid.MissingAccessKey.AsProfileProperties();
            sut.Region = "region1-aws";

            await Assert.ThrowsAsync<ArgumentException>(async () => await sut.ValidateConnectionAsync(toolkitContext));
        }
    }
}
