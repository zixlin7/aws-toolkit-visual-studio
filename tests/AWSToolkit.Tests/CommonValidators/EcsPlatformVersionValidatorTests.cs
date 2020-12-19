using Amazon.AWSToolkit.CommonValidators;
using Xunit;

namespace AWSToolkit.Tests.CommonValidators
{
    public class EcsPlatformVersionValidatorTests
    {
        [Theory]
        [InlineData("LATEST")]
        [InlineData("1.4.0")]
        [InlineData("2.2.0")]
        [InlineData("1.1.0")]
        public void ValidPlatformVersions(string platformVersion)
        {
            Assert.True(string.IsNullOrEmpty(EcsPlatformVersionValidator.Validate(platformVersion)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("LA")]
        [InlineData("abcde")]
        [InlineData("1.2.erer")]
        [InlineData("1.2")]
        [InlineData("1")]
        [InlineData("aa.bb.cc")]
        [InlineData("aa.bb")]
        [InlineData("aa.bb.cc.dd")]
        [InlineData("1.2.1.1")]
        public void InvalidPlatformVersions(string platformVersion)
        {
            Assert.False(string.IsNullOrEmpty(EcsPlatformVersionValidator.Validate(platformVersion)));
        }
    }
}
