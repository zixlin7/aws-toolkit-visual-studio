using System;
using Amazon.AWSToolkit.VisualStudio.Utilities.DTE;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Moq;
using Xunit;

namespace AWSToolkitPackage.Tests.Utilities.DTE
{
    [Collection(UIThreadFixtureCollection.CollectionName)]
    public class ProjectExtensionMethodsTests
    {
        private readonly UIThreadFixture _fixture;
        private readonly Mock<Project> _projectMock = new Mock<Project>();

        public ProjectExtensionMethodsTests(UIThreadFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void SafeGetFileNameHappyPath()
        {
            _projectMock.Setup(x => x.FileName).Returns("foo");
            Assert.Equal("foo", _projectMock.Object.SafeGetFileName("bar"));
        }

        [Fact]
        public void SafeGetFileNameReturnsDefault()
        {
            _projectMock.Setup(x => x.FileName).Returns(() => throw new Exception("nope"));
            Assert.Equal("bar", _projectMock.Object.SafeGetFileName("bar"));
        }

        [Fact]
        public void SafeGetFullNameHappyPath()
        {
            _projectMock.Setup(x => x.FullName).Returns("foo");
            Assert.Equal("foo", _projectMock.Object.SafeGetFullName("bar"));
        }

        [Fact]
        public void SafeGetFullNameReturnsDefault()
        {
            _projectMock.Setup(x => x.FullName).Returns(() => throw new Exception("nope"));
            Assert.Equal("bar", _projectMock.Object.SafeGetFullName("bar"));
        }

        [Fact]
        public void SafeGetUniqueNameHappyPath()
        {
            _projectMock.Setup(x => x.UniqueName).Returns("foo");
            Assert.Equal("foo", _projectMock.Object.SafeGetUniqueName("bar"));
        }

        [Fact]
        public void SafeGetUniqueNameReturnsDefault()
        {
            _projectMock.Setup(x => x.UniqueName).Returns(() => throw new Exception("nope"));
            Assert.Equal("bar", _projectMock.Object.SafeGetUniqueName("bar"));
        }
    }
}