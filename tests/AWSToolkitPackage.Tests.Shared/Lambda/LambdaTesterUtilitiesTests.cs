#if VS2022_OR_LATER
using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.VisualStudio.Lambda;

using AWSToolkitPackage.Tests.Utilities;

using EnvDTE;

using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

using Moq;

using Xunit;

using Task = System.Threading.Tasks.Task;

namespace AWSToolkitPackage.Tests.Lambda
{
    [Collection(TestProjectMockCollection.CollectionName)]
    public class EnsureLambdaTesterConfigured 
    {
        private readonly JoinableTaskFactory _taskFactory;

        public EnsureLambdaTesterConfigured(GlobalServiceProvider globalServiceProvider)
        {
            globalServiceProvider.Reset();

            _taskFactory = ThreadHelper.JoinableTaskContext.Factory;
        }

        [Fact]
        public async Task SolutionIsConfiguredForTester()
        {
            var project = new Mock<Project>();
            project.Setup(x => x.Kind).Returns("csharpproject");
            project.Setup(x => x.FileName).Returns("child-project.csproj");
            var projectList = new List<Project>() { project.Object };

            var projects = new Mock<Projects>();
            projects.Setup(x => x.GetEnumerator()).Returns(() => projectList.GetEnumerator());

            var solution = new Mock<Solution>();
            solution.Setup(x => x.Projects).Returns(projects.Object);

            await LambdaTesterUtilities.EnsureLambdaTesterConfiguredAsync(solution.Object, _taskFactory);

            project.VerifyGet(x => x.FileName, Times.Once);
        }

        [Fact]
        public async Task ProjectIsConfiguredForTester()
        {
            var project = new Mock<Project>();
            project.Setup(x => x.Kind).Returns("csharpproject");
            project.Setup(x => x.FileName).Returns("child-project-1.csproj");

            await LambdaTesterUtilities.EnsureLambdaTesterConfiguredAsync(project.Object, _taskFactory);

            project.VerifyGet(x => x.FileName, Times.Once);
        }

        [Fact]
        public async Task ChildProjectsAreConfiguredForTester()
        {
            // Set up a folder project that contains two non-folder projects
            var childProject1 = new Mock<Project>();
            childProject1.Setup(x => x.Kind).Returns("csharpproject");
            childProject1.Setup(x => x.FileName).Returns("child-project-1.csproj");

            var childProject2 = new Mock<Project>();
            childProject2.Setup(x => x.Kind).Returns("csharpproject");
            childProject2.Setup(x => x.FileName).Returns("child-project-2.csproj");

            var projectItemsMock = CreateProjectItemsMock(childProject1, childProject2);

            var parentFolderProject = new Mock<Project>();
            parentFolderProject.Setup(x => x.Kind).Returns("{66A26720-8FB5-11D2-AA7E-00C04F688DDE}"); // GuidList.VSProjectTypeProjectFolder
            parentFolderProject.Setup(x => x.ProjectItems).Returns(projectItemsMock.Object);

            await LambdaTesterUtilities.EnsureLambdaTesterConfiguredAsync(parentFolderProject.Object, _taskFactory);

            childProject1.VerifyGet(x => x.FileName, Times.Once);
            childProject2.VerifyGet(x => x.FileName, Times.Once);
        }

        private Mock<ProjectItems> CreateProjectItemsMock(params Mock<Project>[] projects)
        {
            var projectItemsList = projects
                .Select(project =>
                {
                    var projectItem = new Mock<ProjectItem>();
                    projectItem.Setup(x => x.SubProject).Returns(project.Object);
                    return projectItem;
                }).ToArray();

            var projectItems = new Mock<ProjectItems>();
            projectItems.Setup(x => x.GetEnumerator()).Returns(() => projectItemsList.Select(x => x.Object).GetEnumerator());
            return projectItems;
        }
    }
}
#endif
