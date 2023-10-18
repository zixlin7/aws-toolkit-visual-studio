using Amazon.AWSToolkit.Credentials.Utils;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Utils
{
    public class ProfilePropertiesTests
    {
        [Fact]
        public void ParseSsoRegistrationScopesReturnsEmptyStringOnNull()
        {
            Assert.Empty(ProfileProperties.ParseSsoRegistrationScopes(null));
        }

        [Fact]
        public void ParseSsoRegistrationScopesReturnsEmptyStringOnWhitespace()
        {
            Assert.Empty(ProfileProperties.ParseSsoRegistrationScopes(" \t \r\n "));
        }

        [Fact]
        public void ParseSsoRegistrationScopesReturnsSingleScope()
        {
            var result = ProfileProperties.ParseSsoRegistrationScopes("  test_scope  ");

            Assert.Single(result);
            Assert.Equal("test_scope", result[0]);
        }

        [Fact]
        public void ParseSsoRegistrationScopesReturnsMultipleScopes()
        {
            var result = ProfileProperties.ParseSsoRegistrationScopes(
                "  test_scope  ,  \t another_scope \r\n,\r\n yet_another_scope \t  ");

            Assert.Equal(3, result.Length);
            Assert.Equal("test_scope", result[0]);
            Assert.Equal("another_scope", result[1]);
            Assert.Equal("yet_another_scope", result[2]);
        }
    }
}
