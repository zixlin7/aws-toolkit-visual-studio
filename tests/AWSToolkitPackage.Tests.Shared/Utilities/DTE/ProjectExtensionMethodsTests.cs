#if VS2022_OR_LATER
using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.VisualStudio.Utilities.DTE;

using EnvDTE;

using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.Utilities.DTE
{
    [Collection(TestProjectMockCollection.CollectionName)]
    public class ProjectExtensionMethodsTests
    {
        private readonly Mock<Project> _projectMock = new Mock<Project>();

        public ProjectExtensionMethodsTests(GlobalServiceProvider globalServiceProvider)
        {
            globalServiceProvider.Reset();
        }

        [Fact]
        public async Task SafeGetFileNameHappyPath()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _projectMock.Setup(x => x.FileName).Returns("foo");
            Assert.Equal("foo", _projectMock.Object.SafeGetFileName("bar"));
        }

        [Fact]
        public async Task SafeGetFileNameReturnsDefault()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _projectMock.Setup(x => x.FileName).Returns(() => throw new Exception("nope"));
            Assert.Equal("bar", _projectMock.Object.SafeGetFileName("bar"));
        }

        [Fact]
        public async Task SafeGetFullNameHappyPath()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _projectMock.Setup(x => x.FullName).Returns("foo");
            Assert.Equal("foo", _projectMock.Object.SafeGetFullName("bar"));
        }

        [Fact]
        public async Task SafeGetFullNameReturnsDefault()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _projectMock.Setup(x => x.FullName).Returns(() => throw new Exception("nope"));
            Assert.Equal("bar", _projectMock.Object.SafeGetFullName("bar"));
        }

        [Fact]
        public async Task SafeGetUniqueNameHappyPath()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _projectMock.Setup(x => x.UniqueName).Returns("foo");
            Assert.Equal("foo", _projectMock.Object.SafeGetUniqueName("bar"));
        }

        [Fact]
        public async Task SafeGetUniqueNameReturnsDefault()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _projectMock.Setup(x => x.UniqueName).Returns(() => throw new Exception("nope"));
            Assert.Equal("bar", _projectMock.Object.SafeGetUniqueName("bar"));
        }
    }
}
#endif
