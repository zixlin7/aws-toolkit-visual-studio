using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
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
    }
}
