using AwsToolkit.VsSdk.Common.LambdaTester;

using Xunit;

namespace AwsToolkit.Vs.Tests.VsSdk.Common.LambdaTester
{
    public class ProjectTests
    {
        [Fact]
        public void AwsProjectType()
        {
            var project = new Project(@"VsSdk.Common\LambdaTester\test-data\LambdaTester-Compatible.csproj");
            Assert.Equal("Lambda", project.AwsProjectType);
        }

        [Fact]
        public void AwsProjectType_NotPresent()
        {
            var project = new Project(@"VsSdk.Common\LambdaTester\test-data\LambdaTester-NoAWSProjectType.csproj");
            Assert.Null(project.AwsProjectType);
        }

        [Fact]
        public void TargetFramework()
        {
            var project = new Project(@"VsSdk.Common\LambdaTester\test-data\LambdaTester-Compatible.csproj");
            Assert.Equal("netcoreapp2.1", project.TargetFramework);
        }

        [Fact]
        public void TargetFramework_NotPresent()
        {
            var project = new Project(@"VsSdk.Common\LambdaTester\test-data\LambdaTester-NoTargetFramework.csproj");
            Assert.Null(project.TargetFramework);
        }
    }
}
