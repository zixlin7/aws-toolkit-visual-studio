using System;
using System.Collections.Generic;
using System.IO;
using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Tests.Common.IO;
using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class ProfileWatcherTests : IDisposable
    {
        private ProfileWatcher _watcher;
        protected readonly TemporaryTestLocation TestLocation = new TemporaryTestLocation();
        private string _filePath;

        [Fact(Timeout = 5000)]
        public void RaisesProfileWatcherChangedEvent()
        {
            DateTime startTime = DateTime.Now;
            double diff = 0;

            _filePath = Path.Combine(TestLocation.TestFolder, "testfile");
            File.WriteAllText(_filePath, "");
            _watcher = new ProfileWatcher(new List<string> {_filePath});
            bool isInvoked = false;
            _watcher.Changed += (s, e) => isInvoked = true;

            Assert.False(isInvoked);
            File.WriteAllText(_filePath, "hello");

            while (!isInvoked && diff < 5000)
            {
                diff = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            }

            if (diff < 5000)
            {
                Assert.True(isInvoked);
            }
        }

        public void Dispose()
        {
            TestLocation.Dispose();
            if (_watcher != null)
            {
                _watcher.Dispose();
            }
        }
    }
}
