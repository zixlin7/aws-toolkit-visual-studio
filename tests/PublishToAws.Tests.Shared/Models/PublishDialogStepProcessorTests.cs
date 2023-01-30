using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class PublishDialogStepProcessorTests
    {
        private static readonly Exception TaskException = new Exception("This action failed");
        private static string SampleDescription1 = "some description";
        private static string SampleDescription2 = "some other description";

        private readonly PublishDialogStepProcessor _sut = new PublishDialogStepProcessor();
        private readonly FakeProgressDialog _progressDialog = new FakeProgressDialog();

        private readonly Func<CancellationToken, Task> _noopTaskCreator = (cancellationToken) => Task.CompletedTask;
        private readonly Func<CancellationToken, Task> _exceptionTaskCreator = (cancellationToken) => throw TaskException;
        private readonly Func<CancellationToken, Task> _cancelDialogTaskCreator;

        public PublishDialogStepProcessorTests()
        {
            _cancelDialogTaskCreator = (cancellationToken) =>
            {
                _progressDialog.CancelRequested = true;
                return Task.CompletedTask;
            };
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdatesCanCancel(bool canCancelValue)
        {
            var steps = new List<ShowPublishDialogStep>()
            {
                new ShowPublishDialogStep(_noopTaskCreator, SampleDescription1, canCancelValue)
            };

            await _sut.ProcessStepsAsync(_progressDialog, steps);

            Assert.Equal(canCancelValue, _progressDialog.CanCancel);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task UpdatesCurrentStep(int totalSteps)
        {
            var steps = Enumerable.Repeat(
                new ShowPublishDialogStep(_noopTaskCreator, SampleDescription1, true), totalSteps).ToList();

            await _sut.ProcessStepsAsync(_progressDialog, steps);

            Assert.Equal(totalSteps, _progressDialog.CurrentStep);
        }

        [Fact]
        public async Task UpdatesHeading1()
        {
            var steps = new List<ShowPublishDialogStep>()
            {
                new ShowPublishDialogStep(_noopTaskCreator, SampleDescription1, true),
                new ShowPublishDialogStep(_noopTaskCreator, SampleDescription2, true),
            };

            await _sut.ProcessStepsAsync(_progressDialog, steps);

            Assert.Equal(steps.Count, _progressDialog.Heading1History.Count);
            Assert.Equal(SampleDescription1, _progressDialog.Heading1History[0]);
            Assert.Equal(SampleDescription2, _progressDialog.Heading1History[1]);
        }

        [Fact]
        public async Task ThrowsIfTaskCancelled()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var steps = new List<ShowPublishDialogStep>()
                {
                    new ShowPublishDialogStep(_noopTaskCreator, SampleDescription1, true),
                    new ShowPublishDialogStep(_noopTaskCreator, SampleDescription2, true),
                };

                _progressDialog.CancellationToken = tokenSource.Token;
                tokenSource.Cancel();

                await Assert.ThrowsAsync<OperationCanceledException>(async () => await _sut.ProcessStepsAsync(_progressDialog, steps));
            }
        }

        [Fact]
        public async Task ThrowsIfDialogCancelled()
        {
            var steps = new List<ShowPublishDialogStep>()
            {
                new ShowPublishDialogStep(_cancelDialogTaskCreator, SampleDescription1, true),
                new ShowPublishDialogStep(_noopTaskCreator, SampleDescription2, true),
            };

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _sut.ProcessStepsAsync(_progressDialog, steps));
        }

        [Fact]
        public async Task ThrowsIfStepThrows()
        {
            var steps = new List<ShowPublishDialogStep>()
            {
                new ShowPublishDialogStep(_exceptionTaskCreator, SampleDescription1, true),
                new ShowPublishDialogStep(_noopTaskCreator, SampleDescription2, true),
            };

            var resultingException = await Assert.ThrowsAsync<Exception>(async () =>
                await _sut.ProcessStepsAsync(_progressDialog, steps));

            Assert.Equal(resultingException, TaskException);
            Assert.Equal(1, _progressDialog.CurrentStep);
        }
    }
}
