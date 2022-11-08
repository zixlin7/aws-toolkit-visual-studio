using Amazon.AWSToolkit.Lambda.LambdaTester;
using Amazon.AWSToolkit.Lambda.Util;
using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class LambdaTesterInstallerTests
    {
        [Theory]
        [InlineData("garbage", null)]
        [InlineData("netcoreapp1.0", null)]
        [InlineData("netcoreapp2.1", "Amazon.Lambda.TestTool-2.1")]
        [InlineData("netcoreapp3.1", "Amazon.Lambda.TestTool-3.1")]
        [InlineData("net5.0", "Amazon.Lambda.TestTool-5.0")]
        [InlineData("net6.0", "Amazon.Lambda.TestTool-6.0")]
        [InlineData("net7.0", "Amazon.Lambda.TestTool-7.0")]
        public void GetTesterConfiguration(string targetFramework, string expectedPackage)
        {
            var configuration = LambdaTesterInstaller.GetTesterConfiguration(targetFramework);
            Assert.Equal(expectedPackage, configuration?.Package);
        }

        [Fact]
        public void IsLambdaTesterSupported_HappyPath()
        {
            var projectFilename = @"Lambda\test-data\LambdaTester-Compatible.csproj";
            var project = new Project(projectFilename);
            Assert.True(LambdaTesterInstaller.IsLambdaTesterSupported(project));
        }

        [Theory]
        [InlineData(@"Lambda\test-data\LambdaTester-LambdaRuntimeSupport.csproj")]
        [InlineData(@"Lambda\test-data\LambdaTester-NoAWSProjectType.csproj")]
        [InlineData(@"Lambda\test-data\LambdaTester-NonLambdaAWSProjectType.csproj")]
        [InlineData(@"Lambda\test-data\LambdaTester-NoTargetFramework.csproj")]
        public void IsLambdaTesterSupported_UnsupportedProjects(string projectFilename)
        {
            var project = new Project(projectFilename);
            Assert.False(LambdaTesterInstaller.IsLambdaTesterSupported(project));
        }
    }
}
