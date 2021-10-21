using System;
using System.Runtime.Versioning;

using Amazon.AWSToolkit.Solutions;
using Amazon.AWSToolkit.Tests.Common.SampleData;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Solutions
{
    public class ProjectTests
    {
        [Fact]
        public void ShouldBeNetCoreProject()
        {
            var project = CreateProjectWithTargetFramework(SampleFrameworkNames.DotNet5);

            Assert.True(project.IsNetCore());
            Assert.False(project.IsNetFramework());
        }

        private Project CreateProjectWithTargetFramework(FrameworkName targetFramework)
        {
            return new Project("SampleProject", @"\my\project\SampleProject.csproj", targetFramework);
        }

        [Fact]
        public void ShouldBeNetFrameworkProject()
        {
            var project = CreateProjectWithTargetFramework(SampleFrameworkNames.DotNetFramework472);

            Assert.True(project.IsNetFramework());
            Assert.False(project.IsNetCore());
        }

        [Fact]
        public void ShouldBeUnknownProject()
        {
            var project = CreateProjectWithTargetFramework(SampleFrameworkNames.Garbage);

            Assert.False(project.IsNetCore());
            Assert.False(project.IsNetFramework());
        }
    }
}
