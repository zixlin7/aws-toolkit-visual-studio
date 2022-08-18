using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Logs.Core;
using Amazon.AWSToolkit.CloudWatch.Logs.Models;
using Amazon.AWSToolkit.CloudWatch.Logs.ViewModels;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Tasks;
using Amazon.CloudWatchLogs;

using AWSToolkit.Tests.CloudWatch.Logs.Fixtures;
using AWSToolkit.Tests.CloudWatch.Logs.Util;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.Logs.ViewModels
{
    public class TestLogEventsViewModel : LogEventsViewModel
    {
        public TestLogEventsViewModel(ICloudWatchLogsRepository repository, ToolkitContext toolkitContext) : base(repository, toolkitContext)
        {
        }

        protected override void ExecuteOnBackgroundThread(Func<Task> function)
        {
            function().LogExceptionAndForget();
        }
    }

    public class LogEventsViewModelTests : BaseLogEntityViewModelTests<LogEventsViewModel>
    {
        private readonly LogEventsViewModelFixture _eventsFixture = new LogEventsViewModelFixture();

        public LogEventsViewModelTests()
        {
            ViewModelFixture = _eventsFixture;
            ViewModel = _eventsFixture.CreateViewModel();
        }

        [Fact]
        public async Task LoadAsync_WhenInitial()
        {
            await ViewModel.LoadAsync();

            Assert.Equal(SampleToken, ViewModel.NextToken);
            Assert.Equal(SampleLogEvents, ViewModel.LogEvents);
            Assert.Equal(SampleLogEvents.First(), ViewModel.LogEvent);
        }

        [Fact]
        public async Task LoadAsync_WhenMorePages()
        {
            var initialPageLogEvents = _eventsFixture.CreateSampleLogEvents();
            var expectedLogEvents = initialPageLogEvents.Concat(SampleLogEvents).ToList();

            await SetupWithInitialLoad("initial-token", initialPageLogEvents);

            _eventsFixture.StubGetLogEventsToReturn(SampleToken, SampleLogEvents);

            await ViewModel.LoadAsync();

            Assert.Equal(SampleToken, ViewModel.NextToken);
            Assert.Equal(expectedLogEvents, ViewModel.LogEvents);
            Assert.Equal(expectedLogEvents.First(), ViewModel.LogEvent);
        }

        [Fact]
        public async Task LoadAsync_WhenLastPage()
        {
            var initialPageLogEvents = _eventsFixture.CreateSampleLogEvents();
            var expectedLogEvents = initialPageLogEvents.Concat(SampleLogEvents).ToList();

            await SetupWithInitialLoad("abc-token", initialPageLogEvents);

            _eventsFixture.StubGetLogEventsToReturn(SampleToken, SampleLogEvents);

            await ViewModel.LoadAsync();

            //for getlogevents, next token remains same when last page is retrieved
            Assert.Equal(SampleToken, ViewModel.NextToken);
            Assert.Equal(expectedLogEvents, ViewModel.LogEvents);
            Assert.Equal(expectedLogEvents.First(), ViewModel.LogEvent);

            Repository.Verify(
                mock => mock.GetLogEventsAsync(It.IsAny<GetLogEventsRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task LoadAsync_WhenNoMorePages()
        {
            await SetupWithInitialLoad(SampleToken, SampleLogEvents);
            await SetupWithInitialLoad(SampleToken, new List<LogEvent>());

            _eventsFixture.StubGetLogEventsToReturn("abc-token", new List<LogEvent>());

            await ViewModel.LoadAsync();

            //for getlogevents, next token remains same when last page is retrieved
            Assert.Equal(SampleToken, ViewModel.NextToken);
            Assert.Equal(SampleLogEvents, ViewModel.LogEvents);

            Repository.Verify(
                mock => mock.GetLogEventsAsync(It.IsAny<GetLogEventsRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }


        [Fact]
        public async Task LoadAsync_Throws()
        {
            Repository.Setup(mock =>
                    mock.GetLogEventsAsync(It.IsAny<GetLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NullReferenceException());

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await ViewModel.LoadAsync();
            });

            Assert.Empty(ViewModel.LogEvents);
            Assert.Null(ViewModel.NextToken);
        }

        [Fact]
        public async Task LoadAsync_TextAndTimeFilter_EmitMetric()
        {
            ApplyTextAndDateTimeFilter();
            await LoadAndVerifyMetric(1, 1);
        }

        [Fact]
        public async Task LoadAsync_TextAndNoTimeFilter_EmitMetric()
        {
            ViewModel.FilterText = "some-filter";
            ViewModel.IsTimeFilterEnabled = true;
            ViewModel.DateTimeRange.StartDate = null;
            ViewModel.DateTimeRange.EndDate = null;
            await LoadAndVerifyMetric(1, 0);
        }

        [Fact]
        public async Task LoadAsync_TextAndDisabledTimeFilter_EmitMetric()
        {
            ApplyTextAndDateTimeFilter();
            ViewModel.IsTimeFilterEnabled = false;
            await LoadAndVerifyMetric(1, 0);
        }

        [Fact]
        public async Task LoadAsync_NoTextAndTimeFilter_EmitMetric()
        {
            ApplyTextAndDateTimeFilter();
            ViewModel.FilterText = string.Empty;
            await LoadAndVerifyMetric(0, 1);
        }

        [Fact]
        public async Task LoadAsync_NoTextAndNoTimeFilter_EmitMetric()
        {
            ViewModel.FilterText = string.Empty;
            ViewModel.IsTimeFilterEnabled = true;
            ViewModel.DateTimeRange.StartDate = null;
            ViewModel.DateTimeRange.EndDate = null;
            await LoadAndVerifyMetric(0, 0);
        }

        [Fact]
        public async Task LoadAsync_NoTextAndDisabledTimeFilter_EmitMetric()
        {
            ViewModel.FilterText = string.Empty;
            ViewModel.IsTimeFilterEnabled = false;
            ViewModel.DateTimeRange.StartDate = DateTime.Now.AddDays(-10);
            ViewModel.DateTimeRange.EndDate = DateTime.Now;
            await LoadAndVerifyMetric(0, 0);
        }

        [Fact]
        public async Task LoadAsyncChangeStartDayFilter_EmitMetric()
        {
            ApplyTextAndDateTimeFilter();
            await LoadAndVerifyMetric(1, 1);

            ViewModel.DateTimeRange.StartDate = DateTime.Now.AddDays(-5);
            await LoadAndVerifyMetric(2, 2);
        }

        [Fact]
        public async Task LoadAsyncChangeStartTimeFilter_EmitMetric()
        {
            ApplyTextAndDateTimeFilter();
            await LoadAndVerifyMetric(1, 1);

            ViewModel.DateTimeRange.StartTimeModel.SetTimeInput((DateTime.MinValue + DateTime.Now.TimeOfDay).AddMinutes(10));
            ViewModel.DateTimeRange.StartTimeModel.SetTime();
            await LoadAndVerifyMetric(2, 2);
        }

        [Fact]
        public async Task LoadAsyncChangeEndDayFilter_EmitMetric()
        {
            ApplyTextAndDateTimeFilter();
            await LoadAndVerifyMetric(1, 1);

            ViewModel.DateTimeRange.EndDate = DateTime.Now.AddDays(-5);
            await LoadAndVerifyMetric(2, 2);
        }

        [Fact]
        public async Task LoadAsyncChangeEndTimeFilter_EmitMetric()
        {
            ApplyTextAndDateTimeFilter();
            await LoadAndVerifyMetric(1, 1);

            ViewModel.DateTimeRange.EndTimeModel.SetTimeInput((DateTime.MinValue + DateTime.Now.TimeOfDay).AddMinutes(10));
            ViewModel.DateTimeRange.EndTimeModel.SetTime();
            await LoadAndVerifyMetric(2, 2);
        }

        private void ApplyTextAndDateTimeFilter()
        {
            ViewModel.FilterText = "some-filter";
            ViewModel.IsTimeFilterEnabled = true;

            ViewModel.DateTimeRange.StartDate = DateTime.Now.AddDays(-10);
            ViewModel.DateTimeRange.StartTimeModel.SetTimeInput(DateTime.MinValue + DateTime.Now.TimeOfDay);

            ViewModel.DateTimeRange.EndDate = DateTime.Now;
            ViewModel.DateTimeRange.EndTimeModel.SetTimeInput(DateTime.MinValue + DateTime.Now.TimeOfDay);
        }

        async Task LoadAndVerifyMetric(int expectedPrefixMetrics, int expectedTimeMetrics)
        {
            await ViewModel.LoadAsync();
            ViewModelFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsFilter(
                ViewModel.GetCloudWatchResourceType(), expectedPrefixMetrics, expectedTimeMetrics);
        }

        [Fact]
        public async Task LoadFilteredAsync_WhenNoMorePages()
        {
            ViewModel.FilterText = "sample-filter";
            await SetupFilteredWithInitialLoad(null, SampleLogEvents);

            _eventsFixture.StubFilterLogEventsToReturn(SampleToken, new List<LogEvent>());

            await ViewModel.LoadAsync();

            Assert.Null(ViewModel.NextToken);
            Assert.Equal(SampleLogEvents, ViewModel.LogEvents);

            Repository.Verify(
                mock => mock.FilterLogEventsAsync(It.IsAny<FilterLogEventsRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task LoadFilteredAsync_WithNoPaginatedLoading()
        {
            ViewModel.FilterText = "sample-filter";

            await ViewModel.LoadAsync();

            Assert.Equal(SampleToken, ViewModel.NextToken);
            Assert.Equal(SampleLogEvents, ViewModel.LogEvents);
            Assert.Equal(SampleLogEvents.First(), ViewModel.LogEvent);

            Assert.Equal(PaginatedLoadingStatus.None, ViewModel.PaginatedLoadingStatus);
        }


        [Fact]
        public async Task PaginatedLoadingContinueCommand_Execute()
        {
            var testViewModel = CreateTestViewModel();
            testViewModel.FilterText = "sample-filter";
            
            _eventsFixture.StubFilterLogEventsToReturn(SampleToken, SampleLogEvents);
            testViewModel.PaginatedLoadingStatus = PaginatedLoadingStatus.LoadMore;

            testViewModel.ContinueLoadingCommand.Execute(null);
            // Give the process some time to execute
            await Task.Delay(1500);

            Assert.Equal(SampleToken, testViewModel.NextToken);
            Assert.Equal(SampleLogEvents, testViewModel.LogEvents);
            Assert.Equal(SampleLogEvents.First(), testViewModel.LogEvent);

            Assert.Equal(PaginatedLoadingStatus.None, testViewModel.PaginatedLoadingStatus);
        }


        [Fact]
        public void PaginatedLoadingContinueCommand_ExecuteCancelled()
        {
            var testViewModel = CreateTestViewModel();
            testViewModel.FilterText = "sample-filter";
            testViewModel.PaginatedLoadingStatus = PaginatedLoadingStatus.None;

            Repository.Setup(mock =>
                    mock.FilterLogEventsAsync(It.IsAny<FilterLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
   

            testViewModel.ContinueLoadingCommand.Execute(null);

            Assert.Null(testViewModel.NextToken);
            Assert.Empty(testViewModel.LogEvents);

            Assert.Equal(PaginatedLoadingStatus.LoadMore, testViewModel.PaginatedLoadingStatus);
            Assert.Empty(testViewModel.ErrorMessage);
        }

        [Fact]
        public void PaginatedLoadingContinueCommand_ExecuteException()
        {
            var testViewModel = CreateTestViewModel();
            testViewModel.FilterText = "sample-filter";

            Repository.Setup(mock =>
                    mock.FilterLogEventsAsync(It.IsAny<FilterLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NullReferenceException());

            testViewModel.ContinueLoadingCommand.Execute(null);

            Assert.Null(testViewModel.NextToken);
            Assert.Empty(testViewModel.LogEvents);

            Assert.Equal(PaginatedLoadingStatus.None, testViewModel.PaginatedLoadingStatus);
            Assert.NotEmpty(testViewModel.ErrorMessage);
        }


        [Fact]
        public void PaginatedLoadingContinueCommand_ExecuteThrottled()
        {
            var testViewModel = CreateTestViewModel();
            testViewModel.FilterText = "sample-filter";
            testViewModel.PaginatedLoadingStatus = PaginatedLoadingStatus.None;


            Repository.Setup(mock =>
                    mock.FilterLogEventsAsync(It.IsAny<FilterLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonCloudWatchLogsException("throttled"){ErrorCode = "ThrottlingException"});

            testViewModel.ContinueLoadingCommand.Execute(null);

            Assert.Null(testViewModel.NextToken);
            Assert.Empty(testViewModel.LogEvents);

            Assert.Equal(PaginatedLoadingStatus.Retry, testViewModel.PaginatedLoadingStatus);
            Assert.NotEmpty(testViewModel.ErrorMessage);
        }


        [Fact]
        public async Task RefreshAsync()
        {
            await SetupWithInitialLoad(SampleToken, SampleLogEvents);

            var newLogEvents = _eventsFixture.CreateSampleLogEvents();
            _eventsFixture.StubGetLogEventsToReturn("refresh-token", newLogEvents);

            await ViewModel.RefreshAsync();

            Assert.Equal("refresh-token", ViewModel.NextToken);
            Assert.Equal(newLogEvents, ViewModel.LogEvents);

            Assert.Equal(newLogEvents.First(), ViewModel.LogEvent);
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public async Task IsTimeFilterEnabled(bool isTimeFilterEnabled, int expectedTimesCalled)
        {
            ViewModel.DateTimeRange.StartDate = new DateTime(2022, 05, 04);
            ViewModel.DateTimeRange.EndDate = new DateTime(2022, 05, 05);

            ViewModel.IsTimeFilterEnabled = isTimeFilterEnabled;
            await ViewModel.LoadAsync();

            Repository.Verify(
                mock => mock.GetLogEventsAsync(It.Is<GetLogEventsRequest>(s => (s.EndTime!=null && s.StartTime!=null)), It.IsAny<CancellationToken>()),
                Times.Exactly(expectedTimesCalled));

        }

        protected override async Task SetupWithInitialLoad()
        {
            await SetupWithInitialLoad(SampleToken, SampleLogEvents);
        }

        /// <summary>
        /// Sets up an initial get load with the specified properties
        /// </summary>
        private async Task SetupWithInitialLoad(string token, List<LogEvent> logEvents)
        {
            _eventsFixture.StubGetLogEventsToReturn(token, logEvents);
            _eventsFixture.StubGetLogEventsToReturn(token, logEvents);
            await ViewModel.LoadAsync();
        }


        /// <summary>
        /// Sets up an initial filtered load with the specified properties
        /// </summary>
        private async Task SetupFilteredWithInitialLoad(string token, List<LogEvent> logEvents)
        {
            _eventsFixture.StubFilterLogEventsToReturn(token, logEvents);
            await ViewModel.LoadAsync();
        }


        public TestLogEventsViewModel CreateTestViewModel()
        {
            var viewModel = new TestLogEventsViewModel(Repository.Object, _eventsFixture.ContextFixture.ToolkitContext)
            {
                LogGroup = _eventsFixture.SampleLogGroup.Name,
                LogStream = _eventsFixture.SampleLogStream.Name
            };
            return viewModel;
        }
    }
}
