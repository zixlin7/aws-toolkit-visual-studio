using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Tasks;
using log4net.Appender;
using log4net.Config;
using log4net.Core;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Tasks
{
    public class SampleLogAppender : MemoryAppender
    {
        public const int TimeoutMs = 3000;

        // LogExceptionAndForget logs async (without waiting).
        // The tests below rely on this event in order to know when something has been logged.
        private readonly ManualResetEvent _entryLoggedEvent = new ManualResetEvent(false);

        public bool WaitForLogEntry()
        {
            return _entryLoggedEvent.WaitOne(TimeoutMs);
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            base.Append(loggingEvent);
            _entryLoggedEvent.Set();
        }
    }

    public class TaskExtensionMethodsTests
    {
        private readonly SampleLogAppender _appender;

        public TaskExtensionMethodsTests()
        {
            _appender = new SampleLogAppender();
            BasicConfigurator.Configure(_appender);
        }

        [Fact]
        public void SyncExceptionIsLogged()
        {
            var task = Task.Run(() => throw new Exception("oops"));
            task.LogExceptionAndForget();

            Assert.True(_appender.WaitForLogEntry());

            Assert.NotEmpty(_appender.GetEvents());
        }

        [Fact]
        public void AsyncExceptionIsLogged()
        {
            var task = Task.Run(async () => { await ThrowSomething(); });
            task.LogExceptionAndForget();

            Assert.True(_appender.WaitForLogEntry());

            Assert.NotEmpty(_appender.GetEvents());
        }

        [Fact]
        public void CancelledTaskIsLogged()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var task = Task.Run(async () => { await Task.Delay(10000, cancellationTokenSource.Token); }, cancellationTokenSource.Token);
            task.LogExceptionAndForget();

            Assert.True(_appender.WaitForLogEntry());

            Assert.NotEmpty(_appender.GetEvents());
        }

        private async Task ThrowSomething()
        {
            await Task.Run(() => {}); // Satisfy async-await requirements for method
            throw new Exception("oops");
        }
    }
}
