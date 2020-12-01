using Amazon.AWSToolkit.CommonValidators;
using Xunit;

namespace AWSToolkit.Tests.CommonValidators
{
    public class EcsRepoNameValidatorTests
    {
        [Theory]
        [InlineData("a")]
        [InlineData("c3repo")]
        [InlineData("c-3-repo")]
        [InlineData("my_repo")]
        [InlineData("my/repo")]
        public void ValidNames(string name)
        {
            Assert.True(string.IsNullOrEmpty(EcsRepoNameValidator.Validate(name)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("A")]
        [InlineData("aAa")]
        [InlineData("a a")]
        [InlineData("3repo")]
        [InlineData("my\\repo")]
        public void InvalidNames(string name)
        {
            Assert.False(string.IsNullOrEmpty(EcsRepoNameValidator.Validate(name)));
        }

        [Fact]
        public void LongName()
        {
            Assert.True(string.IsNullOrEmpty(EcsRepoNameValidator.Validate(new string('x', 256))));
            Assert.False(string.IsNullOrEmpty(EcsRepoNameValidator.Validate(new string('x', 257))));
        }
    }
}