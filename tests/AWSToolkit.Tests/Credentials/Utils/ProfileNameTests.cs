using System;

using Amazon.AWSToolkit.Credentials.Utils;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Utils
{
    public class ProfileNameTests
    {
        [Theory]
        [InlineData("sso-session foo")]
        public void IsSsoSessionShouldPass(string profileName)
        {
            Assert.True(ProfileName.IsSsoSession(profileName));
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("ssosession foo")]
        [InlineData("sso session foo")]
        [InlineData("sso_session foo")]
        [InlineData("sso-sessionfoo")]
        public void IsSsoSessionShouldFail(string profileName)
        {
            Assert.False(ProfileName.IsSsoSession(profileName));
        }

        [Theory]
        [InlineData("sso-session foo", "foo")]
        [InlineData("sso-session   foo", "foo")]
        [InlineData("sso-session   foo     ", "foo")]
        [InlineData("sso-session\tfoo", "foo")]
        public void GetSsoSessionFromProfileName(string profileName, string expectedSsoSession)
        {
            Assert.Equal(expectedSsoSession, ProfileName.GetSsoSessionFromProfileName(profileName));
        }

        [Theory]
        [InlineData("my-profile")]
        [InlineData("sso_session my-profile")]
        [InlineData("sso-session")]
        [InlineData("sso-session     ")]
        public void GetSsoSessionFromProfileNameShouldThrow(string profileName)
        {
            Assert.Throws<ArgumentException>(() => ProfileName.GetSsoSessionFromProfileName(profileName));
        }

        [Fact]
        public void CreateSsoSessionProfileName()
        {
            Assert.Equal("sso-session foo", ProfileName.CreateSsoSessionProfileName("foo"));
        }
    }
}
