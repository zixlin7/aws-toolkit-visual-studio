using Amazon.AWSToolkit.CommonValidators;
using Xunit;

namespace AWSToolkit.Tests.CommonValidators
{
    public class DockerTagValidatorTests
    {
        [Theory]
        [InlineData("a")]
        [InlineData("A")]
        [InlineData("arepo")]
        [InlineData("aRepo")]
        [InlineData("a-repo")]
        [InlineData("a_repo")]
        [InlineData("a.repo")]
        [InlineData("a-repo-3")]
        [InlineData("3-repo")]
        [InlineData("_repo")]
        public void ValidNames(string name)
        {
            Assert.True(string.IsNullOrEmpty(DockerTagValidator.Validate(name)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("a a")]
        [InlineData(".repo")]
        [InlineData("-repo")]
        [InlineData("a#repo")]
        [InlineData("a/repo")]
        [InlineData("a\\repo")]
        public void InvalidNames(string name)
        {
            Assert.False(string.IsNullOrEmpty(DockerTagValidator.Validate(name)));
        }

        [Fact]
        public void LongName()
        {
            Assert.True(string.IsNullOrEmpty(DockerTagValidator.Validate(new string('x', 128))));
            Assert.False(string.IsNullOrEmpty(DockerTagValidator.Validate(new string('x', 129))));
        }
    }
}