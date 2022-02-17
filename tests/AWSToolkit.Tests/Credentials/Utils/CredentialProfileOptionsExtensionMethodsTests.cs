using Amazon.AWSToolkit.Credentials.Utils;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Utils
{
    public class CredentialProfileOptionsExtensionMethodsTests
    {
        [Fact]
        public void ContainsSsoProperties()
        {
            Assert.False(CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Options.ContainsSsoProperties());

            Assert.True(CredentialProfileTestHelper.Sso.ValidProfile.Options.ContainsSsoProperties());
            Assert.True(CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasAccount.Options.ContainsSsoProperties());
            Assert.True(CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasRegion.Options.ContainsSsoProperties());
            Assert.True(CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasRole.Options.ContainsSsoProperties());
            Assert.True(CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasUrl.Options.ContainsSsoProperties());
        }
    }
}
