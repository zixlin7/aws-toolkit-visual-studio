using Amazon.AWSToolkit.Publish;
using Amazon.AWSToolkit.Publish.Install;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Install
{
    public class GetCurrentCliVersionTests
    {
        [Fact]
        public void ParseSampleCliOutput()
        {
            var sampleOutput = @"AWS .NET deployment tool for deploying .NET Core applications to AWS.
Project Home: https://github.com/aws/aws-dotnet-deploy

0.36.8+b044bd05f3";

            Assert.Equal("0.36.8", GetCurrentCliVersion.ParseVersionFromCliOutput(sampleOutput));
        }

        [Theory]
        [InlineData("1.2.3", "1.2.3")]
        [InlineData("\n2.2.2\n1.2.3", "1.2.3")]
        public void ParseNumericVersions(string cliOutput, string expectedVersion)
        {
            Assert.Equal(expectedVersion, GetCurrentCliVersion.ParseVersionFromCliOutput(cliOutput));
        }

        [Theory]
        [InlineData("\n\n")]
        [InlineData("")]
        [InlineData(null)]
        public void ParseEmpty(string cliOutput)
        {
            var ex = Assert.Throws<DeployToolException>(() => GetCurrentCliVersion.ParseVersionFromCliOutput(cliOutput));
            Assert.Equal(ex.Message, GetCurrentCliVersion.ErrorMessages.VersionOutputEmpty);
        }
    }
}
