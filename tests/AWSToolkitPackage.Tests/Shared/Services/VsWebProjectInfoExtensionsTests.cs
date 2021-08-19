using Amazon.AWSToolkit.Solutions;
using Amazon.AWSToolkit.VisualStudio.Services;
using Amazon.AwsToolkit.VsSdk.Common;

using Xunit;

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
            var expectedProject = new Project(expectedProjectType);

            // act + assert.
            Assert.Equal(expectedProject, projectInfo.AsProject());
        }
    }
}
