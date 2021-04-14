using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime.CredentialManagement;
using Xunit;

namespace AWSToolkit.Tests.Credentials.Utils
{
    public class CredentialProfileOptionsExtensionMethodsTests
    {
        [Fact]
        public void ContainsSsoProperties()
        {
            Assert.False(CredentialProfileTestHelper.BasicProfile.Options.ContainsSsoProperties());

            Assert.True(CredentialProfileTestHelper.SSOProfile.Options.ContainsSsoProperties());
            Assert.True(CredentialProfileTestHelper.InvalidSSOProfileOnlyAccount.Options.ContainsSsoProperties());
            Assert.True(CredentialProfileTestHelper.InvalidSSOProfileOnlyRegion.Options.ContainsSsoProperties());
            Assert.True(CredentialProfileTestHelper.InvalidSSOProfileOnlyRole.Options.ContainsSsoProperties());
            Assert.True(CredentialProfileTestHelper.InvalidSSOProfileOnlyUrl.Options.ContainsSsoProperties());
        }
    }
}
