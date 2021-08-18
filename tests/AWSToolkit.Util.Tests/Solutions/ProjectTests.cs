using Amazon.AWSToolkit.Solutions;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Solutions
{
    public class ProjectTests
    {
        [Fact]
        public void ShouldBeNetCoreProject()
        {
            var project = new Project(ProjectType.NetCore);

            Assert.True(project.IsNetCore());
            Assert.False(project.IsNetFramework());
            Assert.False(project.IsUnknown());
        }

        [Fact]
        public void ShouldBeNetFrameworkProject()
        {
            var project = new Project(ProjectType.NetFramework);

            Assert.True(project.IsNetFramework());
            Assert.False(project.IsNetCore());
            Assert.False(project.IsUnknown());
        }

        [Fact]
        public void ShouldBeUnknownProject()
        {
            var project = new Project(ProjectType.Unknown);

            Assert.True(project.IsUnknown());
            Assert.False(project.IsNetCore());
            Assert.False(project.IsNetFramework());
        }
    }
}
