using Amazon;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime.CredentialManagement;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Utils
{
    public class CredentialProfileExtensionMethodsTests
    {
        [Fact]
        public void AsProfileProperties()
        {
            var profile = new CredentialProfile("sample", new CredentialProfileOptions { AccessKey = "accessKey" })
            {
                Region = RegionEndpoint.USEast1
            };
            var properties = profile.AsProfileProperties();
            Assert.NotNull(properties);
            Assert.Equal("sample", properties.Name);
            Assert.Equal(RegionEndpoint.USEast1.SystemName, properties.Region);
            Assert.Null(properties.UniqueKey);
            Assert.Equal("accessKey", properties.AccessKey);
            Assert.Empty(properties.SsoAccountId);
            Assert.Empty(properties.SsoRegion);
            Assert.Empty(properties.SsoRoleName);
            Assert.Empty(properties.SsoStartUrl);
        }

        [Fact]
        public void AsProfileProperties_SessionToken()
        {
            var profile = CredentialProfileTestHelper.SessionProfile;
            var properties = profile.AsProfileProperties();

            Assert.NotNull(properties);
            Assert.Equal(profile.Options.AccessKey, properties.AccessKey);
            Assert.Equal(profile.Options.SecretKey, properties.SecretKey);
            Assert.Equal(profile.Options.Token, properties.Token);
            Assert.Empty(properties.CredentialProcess);
            Assert.Empty(properties.RoleArn);
            Assert.Empty(properties.MfaSerial);
            Assert.Empty(properties.SsoAccountId);
            Assert.Empty(properties.SsoRegion);
            Assert.Empty(properties.SsoRoleName);
            Assert.Empty(properties.SsoStartUrl);
        }

        [Fact]
        public void AsProfileProperties_CredentialProcess()
        {
            var profile = CredentialProfileTestHelper.CredentialProcessProfile;
            var properties = profile.AsProfileProperties();

            Assert.NotNull(properties);
            Assert.Empty(properties.AccessKey);
            Assert.Empty(properties.SecretKey);
            Assert.Empty(properties.Token);
            Assert.Equal(profile.Options.CredentialProcess, properties.CredentialProcess);
            Assert.Empty(properties.RoleArn);
            Assert.Empty(properties.MfaSerial);
            Assert.Empty(properties.SsoAccountId);
            Assert.Empty(properties.SsoRegion);
            Assert.Empty(properties.SsoRoleName);
            Assert.Empty(properties.SsoStartUrl);
        }

        [Fact]
        public void AsProfileProperties_Mfa()
        {
            var profile = CredentialProfileTestHelper.MFAProfile;
            var properties = profile.AsProfileProperties();

            Assert.NotNull(properties);
            Assert.Empty(properties.AccessKey);
            Assert.Empty(properties.SecretKey);
            Assert.Empty(properties.Token);
            Assert.Empty(properties.CredentialProcess);
            Assert.Equal(profile.Options.RoleArn, properties.RoleArn);
            Assert.Equal(profile.Options.MfaSerial, properties.MfaSerial);
            Assert.Empty(properties.SsoAccountId);
            Assert.Empty(properties.SsoRegion);
            Assert.Empty(properties.SsoRoleName);
            Assert.Empty(properties.SsoStartUrl);
        }

        [Fact]
        public void AsProfileProperties_Sso()
        {
            var profile = CredentialProfileTestHelper.SSOProfile;
            var properties = profile.AsProfileProperties();

            Assert.NotNull(properties);
            Assert.Empty(properties.AccessKey);
            Assert.Empty(properties.SecretKey);
            Assert.Empty(properties.Token);
            Assert.Empty(properties.CredentialProcess);
            Assert.Empty(properties.RoleArn);
            Assert.Empty(properties.MfaSerial);
            Assert.Equal(profile.Options.SsoAccountId, properties.SsoAccountId);
            Assert.Equal(profile.Options.SsoRegion, properties.SsoRegion);
            Assert.Equal(profile.Options.SsoRoleName, properties.SsoRoleName);
            Assert.Equal(profile.Options.SsoStartUrl, properties.SsoStartUrl);
        }

        [Fact]
        public void AsProfileProperties_WhenProfileNull()
        {
            CredentialProfile profile = null;
            Assert.Null(profile.AsProfileProperties());
        }
    }
}
