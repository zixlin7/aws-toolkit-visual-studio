using Amazon.AWSToolkit.Solutions;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Solutions
{
    public class ProjectFileTests
    {
        [Theory]
        [InlineData("net5.0", "AspNet.csproj")]
        [InlineData("net5.0", "FSharp.fsproj")]
        [InlineData("net5.0", "LambdaTester-Compatible.csproj")]
        [InlineData(null, "DotNetFrameworkProject.csproj")]
        [InlineData("netstandard2.1", "DotNetStandard.csproj")]
        [InlineData(null, "VisualBasic.vbproj")]
        [InlineData(null, "NoTargetFramework.csproj")]
        public void ShouldGetTargetFramework(string expected, string filename)
        {
            var reader = new ProjectFile(GetProjectPath(filename));
            Assert.Equal(expected, reader.TargetFramework);
        }

        private string GetProjectPath(string filename)
        {
            return $@"Solutions\SampleProjects\{filename}";
        }
    }
}
