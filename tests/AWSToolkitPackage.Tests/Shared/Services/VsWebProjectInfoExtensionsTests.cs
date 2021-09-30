using Amazon.AWSToolkit.Solutions;
using Amazon.AWSToolkit.VisualStudio.Services;
using Amazon.AwsToolkit.VsSdk.Common;

using Moq;

using Xunit;

using Project = Amazon.AWSToolkit.Solutions.Project;

namespace AWSToolkitPackage.Tests.Shared.Services
{
    public class VsWebProjectInfoExtensionsTests
    {
        [Theory]
        [InlineData(VSWebProjectInfo.guidAWSPrivateCoreCLRWebProject, ProjectType.NetCore)]
        [InlineData(VSWebProjectInfo.guidWebApplicationProject, ProjectType.NetFramework)]
        [InlineData("aaaa-aaaa-aaaa-aaaa", ProjectType.Unknown)]
        public void ShouldGetCorrectProjectWithType(string projectTypeGuid, ProjectType expectedProjectType)
        {
            // arrange.
            var projectInfo = new VSWebProjectInfo(null, "", projectTypeGuid);
            var expectedProject = new Project(null, null, expectedProjectType);

            // act + assert.
            Assert.Equal(expectedProject, projectInfo.AsProject());
        }

        [Fact]
        public void ShouldGetCorrectProjectInfo()
        {
            // arrange.
            string name = "SampleProject";
            string path = @"my\project\SampleProject.csproj";

            var projectInfo = CreateWebProjectInfoWith(name, path, VSWebProjectInfo.guidAWSPrivateCoreCLRWebProject);

            var expectedProject = new Project(name, path, ProjectType.NetCore);

            // act + assert.
            Assert.Equal(expectedProject, projectInfo.AsProject());
        }

        private VSWebProjectInfo CreateWebProjectInfoWith(string name, string path, string projectTypeGuid)
        {
            var dteProject = CreateDTEProjectWith(name, path);
            return new VSWebProjectInfoWithDTEProject(dteProject, projectTypeGuid);
        }

        private EnvDTE.Project CreateDTEProjectWith(string name, string path)
        {
            var dteProject = new Mock<EnvDTE.Project>();
            dteProject.SetupGet(m => m.Name).Returns(name);
            dteProject.SetupGet(m => m.FileName).Returns(path);
            return dteProject.Object;
        }

        public class VSWebProjectInfoWithDTEProject : VSWebProjectInfo
        {
            public override EnvDTE.Project DTEProject { get; }

            public VSWebProjectInfoWithDTEProject(EnvDTE.Project dteProject, string projectTypeGuid) : base(null, "", projectTypeGuid)
            {
                DTEProject = dteProject;
            }
        }
    }
}
