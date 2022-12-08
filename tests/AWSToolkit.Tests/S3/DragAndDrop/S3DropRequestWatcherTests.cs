using System;
using System.IO;
using System.Threading;

using Amazon.AWSToolkit.S3.DragAndDrop;
using Amazon.AWSToolkit.Tests.Common.IO;

using Xunit;

namespace AWSToolkit.Tests.S3.DragAndDrop
{
    public class S3DropRequestWatcherTests : IDisposable
    {
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly string _dropTargetManifestPath;
        private readonly string _dropSourceManifestPath;
        private readonly S3DropRequestWatcher _sut;

        public S3DropRequestWatcherTests()
        {
            var manifestFilename = $"manifest-{Guid.NewGuid().ToString()}";

            _dropTargetManifestPath = Path.Combine(_testLocation.TestFolder, manifestFilename);
            _dropSourceManifestPath = Path.Combine(_testLocation.TestFolder, $"manifest-source");

            _sut = new S3DropRequestWatcher(manifestFilename, _dropSourceManifestPath);
        }

        [Fact]
        public void ShouldRaiseDropRequest()
        {
            S3DropRequestEventArgs eventArgs = null;
            ManualResetEvent raisedEvent = new ManualResetEvent(false );

            _sut.DropRequest += (sender, s3DropRequest) =>
            {
                eventArgs = s3DropRequest;
                raisedEvent.Set();
            };

            SimulateS3ObjectDrop();

            // It can take a while to get the event, but don't wait too long
            Assert.True(raisedEvent.WaitOne(TimeSpan.FromSeconds(2)));
            Assert.Equal(_dropTargetManifestPath, eventArgs.FilePath);
        }

        [Fact]
        public void DropSourceShouldNotRaiseDropRequest()
        {
            bool raised = false;
            _sut.DropRequest += (sender, request) => raised = true;
            File.WriteAllText(_dropSourceManifestPath, "foo");

            Assert.False(raised);
        }

        [Fact]
        public void ShouldRaiseOneDropRequest()
        {
            ManualResetEvent raisedEvent = new ManualResetEvent(false);
            int timesRaised = 0;
            _sut.DropRequest += (sender, request) =>
            {
                timesRaised++;
                raisedEvent.Set();
            };

            SimulateS3ObjectDrop();

            // repeatedly delete and re-create the file a couple of times
            for (int i = 0; i < 5; i++)
            {
                File.Delete(_dropTargetManifestPath);
                SimulateS3ObjectDrop();
            }

            Assert.True(raisedEvent.WaitOne(TimeSpan.FromSeconds(2)));
            Assert.Equal(1, timesRaised);
        }

        private void SimulateS3ObjectDrop()
        {
            File.WriteAllText(_dropTargetManifestPath, "foo");
        }

        public void Dispose()
        {
            _sut?.Dispose();
            _testLocation?.Dispose();
        }
    }
}
