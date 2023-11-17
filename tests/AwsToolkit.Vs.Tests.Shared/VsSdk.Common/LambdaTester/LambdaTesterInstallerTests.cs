using AwsToolkit.VsSdk.Common.LambdaTester;

using Xunit;

namespace AwsToolkit.Vs.Tests.VsSdk.Common.LambdaTester
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
            var projectFilename = @"VsSdk.Common\LambdaTester\test-data\LambdaTester-Compatible.csproj";
            var project = new Project(projectFilename);
            Assert.True(LambdaTesterInstaller.IsLambdaTesterSupported(project));
        }

        [Theory]
        [InlineData(@"VsSdk.Common\LambdaTester\test-data\LambdaTester-LambdaRuntimeSupport.csproj")]
        [InlineData(@"VsSdk.Common\LambdaTester\test-data\LambdaTester-NoAWSProjectType.csproj")]
        [InlineData(@"VsSdk.Common\LambdaTester\test-data\LambdaTester-NonLambdaAWSProjectType.csproj")]
        [InlineData(@"VsSdk.Common\LambdaTester\test-data\LambdaTester-NoTargetFramework.csproj")]
        public void IsLambdaTesterSupported_UnsupportedProjects(string projectFilename)
        {
            var project = new Project(projectFilename);
            Assert.False(LambdaTesterInstaller.IsLambdaTesterSupported(project));
        }
    }
}
