using Amazon.AWSToolkit.Solutions;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Solutions
{
    public class ProjectTests
    {
        [Fact]
        public void ShouldBeNetCoreProject()
        {
            var project = CreateProjectWithType(ProjectType.NetCore);

            Assert.True(project.IsNetCore());
            Assert.False(project.IsNetFramework());
            Assert.False(project.IsUnknown());
        }

        private Project CreateProjectWithType(ProjectType type)
        {
            return new Project("SampleProject", @"\my\project\SampleProject.csproj", type);
        }

        [Fact]
        public void ShouldBeNetFrameworkProject()
        {
            var project = CreateProjectWithType(ProjectType.NetFramework);

            Assert.True(project.IsNetFramework());
            Assert.False(project.IsNetCore());
            Assert.False(project.IsUnknown());
        }

        [Fact]
        public void ShouldBeUnknownProject()
        {
            var project = CreateProjectWithType(ProjectType.Unknown);

            Assert.True(project.IsUnknown());
            Assert.False(project.IsNetCore());
            Assert.False(project.IsNetFramework());
        }
    }
}
