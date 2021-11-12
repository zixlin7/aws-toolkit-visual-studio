using System;
using System.Collections;
using System.Linq;
using Amazon.AWSToolkit.Lambda;
using Amazon.AWSToolkit.VisualStudio.Lambda;
using EnvDTE;
using Microsoft.VisualStudio.Threading;

using Moq;
using Xunit;

using Task = System.Threading.Tasks.Task;

namespace AWSToolkitPackage.Tests.Lambda
{
    [Collection(UIThreadFixtureCollection.CollectionName)]
    public class EnsureLambdaTesterConfigured 
    {
        private readonly UIThreadFixture _fixture;
        private readonly Mock<IAWSLambda> _lambdaPluginMock = new Mock<IAWSLambda>();
        private readonly JoinableTaskFactory _taskFactory;

        public EnsureLambdaTesterConfigured(UIThreadFixture fixture)
        {
            _fixture = fixture;
#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskCollection = new JoinableTaskContext();
#pragma warning restore VSSDK005
            _taskFactory = taskCollection.Factory;
        }

        [Fact]
        public async Task ThrowsExceptionIfLambdaPluginIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                Project project = null;
                await LambdaTesterUtilities.EnsureLambdaTesterConfiguredAsync(project, null, _taskFactory);
            });
        }

        [Fact]
        public async Task SolutionIsConfiguredForTester()
        {
            var project = new Mock<Project>();
            project.Setup(x => x.Kind).Returns("csharpproject");
            project.Setup(x => x.FileName).Returns("child-project.csproj");

            var projects = CreateEnumerableMock<Project, Projects>(project);

            var solution = new Mock<Solution>();
            solution.Setup(x => x.Projects).Returns(projects.Object);

            await LambdaTesterUtilities.EnsureLambdaTesterConfiguredAsync(solution.Object, _lambdaPluginMock.Object, _taskFactory);

            _lambdaPluginMock.Verify(x => x.EnsureLambdaTesterConfiguredAsync(project.Object.FileName), Times.Once);
        }

        [Fact]
        public async Task ProjectIsConfiguredForTester()
        {
            var project = new Mock<Project>();
            project.Setup(x => x.Kind).Returns("csharpproject");
            project.Setup(x => x.FileName).Returns("child-project-1.csproj");

            await LambdaTesterUtilities.EnsureLambdaTesterConfiguredAsync(project.Object, _lambdaPluginMock.Object, _taskFactory);

            _lambdaPluginMock.Verify(x => x.EnsureLambdaTesterConfiguredAsync(project.Object.FileName), Times.Once);
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

            await LambdaTesterUtilities.EnsureLambdaTesterConfiguredAsync(parentFolderProject.Object, _lambdaPluginMock.Object, _taskFactory);

            _lambdaPluginMock.Verify(x => x.EnsureLambdaTesterConfiguredAsync(childProject1.Object.FileName), Times.Once);
            _lambdaPluginMock.Verify(x => x.EnsureLambdaTesterConfiguredAsync(childProject2.Object.FileName), Times.Once);
        }

        private Mock<ProjectItems> CreateProjectItemsMock(params Mock<Project>[] projects)
        {
            var projectItems = projects
                .Select(project =>
                {
                    var projectItem = new Mock<ProjectItem>();
                    projectItem.Setup(x => x.SubProject).Returns(project.Object);
                    return projectItem;
                }).ToArray();

            return CreateEnumerableMock<ProjectItem, ProjectItems>(projectItems);
        }

        private Mock<TOut> CreateEnumerableMock<TIn, TOut>(params Mock<TIn>[] items)
            where TIn : class
            where TOut : class, IEnumerable
        {
            var projectItemsMock = new Mock<TOut>();
            projectItemsMock.Setup(x => x.GetEnumerator()).Returns(items.Select(x => x.Object).GetEnumerator());

            return projectItemsMock;
        }
    }
}
