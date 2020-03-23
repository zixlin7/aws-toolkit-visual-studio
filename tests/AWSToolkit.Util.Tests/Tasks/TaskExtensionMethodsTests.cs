using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Tasks;
using log4net.Appender;
using log4net.Config;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Tasks
{
    public class TaskExtensionMethodsTests
    {
        private readonly MemoryAppender appender;

        public TaskExtensionMethodsTests()
        {
            appender = new MemoryAppender();
            BasicConfigurator.Configure(appender);
        }

        [Fact]
        public async Task SyncExceptionIsLogged()
        {
            var task = Task.Run(() => throw new Exception("oops"));
            task.LogExceptionAndForget();

            await WaitForTaskToComplete(task);

            Assert.NotEmpty(appender.GetEvents());
        }

        [Fact]
        public async Task AsyncExceptionIsLogged()
        {
            var task = Task.Run(async () => { await ThrowSomething(); });
            task.LogExceptionAndForget();

            await WaitForTaskToComplete(task);

            Assert.NotEmpty(appender.GetEvents());
        }

        [Fact]
        public async Task CancelledTaskIsLogged()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var task = Task.Run(async () => { await Task.Delay(10000, cancellationTokenSource.Token); }, cancellationTokenSource.Token);
            task.LogExceptionAndForget();

            await WaitForTaskToComplete(task);

            Assert.NotEmpty(appender.GetEvents());
        }

        private static async Task WaitForTaskToComplete(Task task)
        {
            while (!task.IsCompleted)
            {
                await Task.Delay(200);
            }
        }

        private async Task ThrowSomething()
        {
            await Task.Run(() => {}); // Satisfy async-await requirements for method
            throw new Exception("oops");
        }
    }
}