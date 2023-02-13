using System;
using System.IO;

using Amazon.AWSToolkit.S3.DragAndDrop;
using Amazon.AWSToolkit.Tests.Common.IO;

using Xunit;

namespace AWSToolkit.Tests.S3.DragAndDrop
{
    public class DropSourcePlaceholderTests : IDisposable
    {
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly string _manifestPath;
        private readonly S3DragDropManifest _manifest;
        private readonly DropSourcePlaceholder _sut;

        public DropSourcePlaceholderTests()
        {
            var requestId = Guid.NewGuid().ToString();
            _manifest = new S3DragDropManifest() { RequestId = requestId };
            _manifestPath = Path.Combine(_testLocation.TestFolder, $"{requestId}.txt");
            _sut = new DropSourcePlaceholder(_manifestPath);
        }

        [Fact]
        public void ShouldCreateFile()
        {
            _sut.Create(_manifest);

            Assert.True(File.Exists(_manifestPath));
            Assert.Equal(_manifest.RequestId, S3DragDropManifest.Load(_manifestPath).RequestId);
        }

        [Fact]
        public void ShouldDeleteFile()
        {
            _sut.Create(_manifest);
            _sut.Dispose();

            Assert.False(File.Exists(_manifestPath));
        }

        [Fact]
        public void ShouldNotOverwritePreExistingFile()
        {
            // Setup
            File.WriteAllText(_manifestPath, "foo");

            // Act
            _sut.Create(_manifest);

            Assert.Equal("foo", File.ReadAllText(_manifestPath));
        }

        [Fact]
        public void ShouldNotDeletePreExistingFile()
        {
            // Setup
            File.WriteAllText(_manifestPath, "foo");
            _sut.Create(_manifest);

            // Act
            _sut.Dispose();

            Assert.True(File.Exists(_manifestPath));
        }

        public void Dispose()
        {
            _sut.Dispose();
            _testLocation?.Dispose();
        }
    }
}
