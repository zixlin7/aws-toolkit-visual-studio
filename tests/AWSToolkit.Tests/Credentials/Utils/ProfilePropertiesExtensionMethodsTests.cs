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
                CredentialProfileTestHelper.MFAProfile.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_AssumeRoleExternalMfa()
        {
            Assert.Equal(CredentialType.AssumeMfaRoleProfile,
                CredentialProfileTestHelper.MFAExternalSessionProfile.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_AssumeRole()
        {
            Assert.Equal(CredentialType.AssumeRoleProfile,
                CredentialProfileTestHelper.AssumeRoleProfile.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_SessionToken()
        {
            Assert.Equal(CredentialType.StaticSessionProfile,
                CredentialProfileTestHelper.SessionProfile.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_Static()
        {
            Assert.Equal(CredentialType.StaticProfile,
                CredentialProfileTestHelper.BasicProfile.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_CredentialProcess()
        {
            Assert.Equal(CredentialType.CredentialProcessProfile,
                CredentialProfileTestHelper.CredentialProcessProfile.AsProfileProperties().GetCredentialType());
        }

        [Fact]
        public void GetCredentialType_Sso()
        {
            Assert.Equal(CredentialType.SsoProfile,
                CredentialProfileTestHelper.SSOProfile.AsProfileProperties().GetCredentialType());

            var profileProperties = new ProfileProperties {SsoAccountId = "sso-account"};
            Assert.Equal(CredentialType.SsoProfile, profileProperties.GetCredentialType());

            profileProperties = new ProfileProperties {SsoRegion = "sso-region"};
            Assert.Equal(CredentialType.SsoProfile, profileProperties.GetCredentialType());

            profileProperties = new ProfileProperties {SsoRoleName = "sso-role"};
            Assert.Equal(CredentialType.SsoProfile, profileProperties.GetCredentialType());

            profileProperties = new ProfileProperties {SsoStartUrl = "sso-url"};
            Assert.Equal(CredentialType.SsoProfile, profileProperties.GetCredentialType());
        }
    }
}
