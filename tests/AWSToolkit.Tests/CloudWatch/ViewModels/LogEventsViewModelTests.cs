using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Tasks;

using AWSToolkit.Tests.CloudWatch.Fixtures;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.ViewModels
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

    public class LogEventsViewModelTests
    {
        private readonly LogEventsViewModel _viewModel;
        private readonly LogEventsViewModelFixture _eventsFixture = new LogEventsViewModelFixture();
        private string SampleToken => _eventsFixture.SampleToken;
        private List<LogEvent> SampleLogEvents => _eventsFixture.SampleLogEvents;
        private Mock<ICloudWatchLogsRepository> Repository => _eventsFixture.Repository;

        public LogEventsViewModelTests()
        {
            _viewModel = _eventsFixture.CreateViewModel();
        }

        [Fact]
        public async Task LoadAsync_WhenInitial()
        {
            await _viewModel.LoadAsync();

            Assert.Equal(SampleToken, _viewModel.NextToken);
            Assert.Equal(SampleLogEvents, _viewModel.LogEvents);
            Assert.Equal(SampleLogEvents.First(), _viewModel.LogEvent);
        }

        [Fact]
        public async Task LoadAsync_AdjustsLoadingLogs()
        {
            var loadingAdjustments= new List<bool>();

            _viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(_viewModel.LoadingLogs))
                {
                    loadingAdjustments.Add(_viewModel.LoadingLogs);
                }
            };

            Assert.False(_viewModel.LoadingLogs);

            await _viewModel.LoadAsync();

            Assert.Equal(2, loadingAdjustments.Count);
            Assert.True(loadingAdjustments[0]);
            Assert.False(loadingAdjustments[1]);
        }

        [Fact]
        public async Task LoadAsync_WhenMorePages()
        {
            var initialPageLogEvents = _eventsFixture.CreateSampleLogEvents();
            var expectedLogEvents = initialPageLogEvents.Concat(SampleLogEvents).ToList();

            await SetupWithInitialLoad("initial-token", initialPageLogEvents);

            _eventsFixture.StubGetLogEventsToReturn(SampleToken, SampleLogEvents);

            await _viewModel.LoadAsync();

            Assert.Equal(SampleToken, _viewModel.NextToken);
            Assert.Equal(expectedLogEvents, _viewModel.LogEvents);
            Assert.Equal(expectedLogEvents.First(), _viewModel.LogEvent);
        }

        [Fact]
        public async Task LoadAsync_WhenLastPage()
        {
            var initialPageLogEvents = _eventsFixture.CreateSampleLogEvents();
            var expectedLogEvents = initialPageLogEvents.Concat(SampleLogEvents).ToList();

            await SetupWithInitialLoad("abc-token", initialPageLogEvents);

            _eventsFixture.StubGetLogEventsToReturn(SampleToken, SampleLogEvents);

            await _viewModel.LoadAsync();

            //for getlogevents, next token remains same when last page is retrieved
            Assert.Equal(SampleToken, _viewModel.NextToken);
            Assert.Equal(expectedLogEvents, _viewModel.LogEvents);
            Assert.Equal(expectedLogEvents.First(), _viewModel.LogEvent);

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

            await _viewModel.LoadAsync();

            //for getlogevents, next token remains same when last page is retrieved
            Assert.Equal(SampleToken, _viewModel.NextToken);
            Assert.Equal(SampleLogEvents, _viewModel.LogEvents);

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
                await _viewModel.LoadAsync();
            });

            Assert.Empty(_viewModel.LogEvents);
            Assert.Null(_viewModel.NextToken);
        }

        [Fact]
        public async Task LoadFilteredAsync_WhenNoMorePages()
        {
            _viewModel.FilterText = "sample-filter";
            await SetupFilteredWithInitialLoad(null, SampleLogEvents);

            _eventsFixture.StubFilterLogEventsToReturn(SampleToken, new List<LogEvent>());

            await _viewModel.LoadAsync();

            Assert.Null(_viewModel.NextToken);
            Assert.Equal(SampleLogEvents, _viewModel.LogEvents);

            Repository.Verify(
                mock => mock.FilterLogEventsAsync(It.IsAny<FilterLogEventsRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task LoadFilteredAsync_WithNoPaginatedLoading()
        {
            _viewModel.FilterText = "sample-filter";

            await _viewModel.LoadAsync();

            Assert.Equal(SampleToken, _viewModel.NextToken);
            Assert.Equal(SampleLogEvents, _viewModel.LogEvents);
            Assert.Equal(SampleLogEvents.First(), _viewModel.LogEvent);

            Assert.Equal(PaginatedLoadingStatus.None, _viewModel.PaginatedLoadingStatus);
        }

        [Fact]
        public async Task LoadFilteredAsync_WithPaginatedLoadingPrompt()
        {
            _viewModel.FilterText = "sample-filter";

            await SetupFilteredWithInitialLoad(SampleToken, new List<LogEvent>());

            await _viewModel.LoadAsync();

            Assert.Equal(SampleToken, _viewModel.NextToken);
            Assert.Empty( _viewModel.LogEvents);
            Assert.Null(_viewModel.LogEvent);

            Assert.Equal(PaginatedLoadingStatus.Prompt, _viewModel.PaginatedLoadingStatus);
        }

        [Fact]
        public void PaginatedLoadingContinueCommand_Execute()
        {
            var testViewModel = CreateTestViewModel();
            testViewModel.FilterText = "sample-filter";
            var paginatedLoadingStatus = new List<PaginatedLoadingStatus>();

            testViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(testViewModel.PaginatedLoadingStatus))
                {
                    paginatedLoadingStatus.Add(testViewModel.PaginatedLoadingStatus);
                }
            };

            _eventsFixture.StubFilterLogEventsToReturn(SampleToken, SampleLogEvents);

            testViewModel.ContinueLoadingCommand.Execute(null);

            Assert.Equal(SampleToken, testViewModel.NextToken);
            Assert.Equal(SampleLogEvents, testViewModel.LogEvents);
            Assert.Equal(SampleLogEvents.First(), testViewModel.LogEvent);

            Assert.Equal(2, paginatedLoadingStatus.Count);
            Assert.Equal(PaginatedLoadingStatus.Loading, paginatedLoadingStatus[0]);
            Assert.Equal(PaginatedLoadingStatus.None, paginatedLoadingStatus[1]);
        }


        [Fact]
        public void PaginatedLoadingContinueCommand_ExecuteCancelled()
        {
            var testViewModel = CreateTestViewModel();
            testViewModel.FilterText = "sample-filter";
            var paginatedLoadingStatus = new List<PaginatedLoadingStatus>();

            testViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(testViewModel.PaginatedLoadingStatus))
                {
                    paginatedLoadingStatus.Add(testViewModel.PaginatedLoadingStatus);
                }
            };

            Repository.Setup(mock =>
                    mock.FilterLogEventsAsync(It.IsAny<FilterLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            testViewModel.ContinueLoadingCommand.Execute(null);

            Assert.Null(testViewModel.NextToken);
            Assert.Empty( testViewModel.LogEvents);

            Assert.Equal(2, paginatedLoadingStatus.Count);
            Assert.Equal(PaginatedLoadingStatus.Loading, paginatedLoadingStatus[0]);
            Assert.Equal(PaginatedLoadingStatus.Prompt, paginatedLoadingStatus[1]);
            Assert.Empty(testViewModel.ErrorMessage);
        }

        [Fact]
        public void PaginatedLoadingContinueCommand_ExecuteException()
        {
            var testViewModel = CreateTestViewModel();
            testViewModel.FilterText = "sample-filter";
            var paginatedLoadingStatus = new List<PaginatedLoadingStatus>();

            testViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(testViewModel.PaginatedLoadingStatus))
                {
                    paginatedLoadingStatus.Add(testViewModel.PaginatedLoadingStatus);
                }
            };

            Repository.Setup(mock =>
                    mock.FilterLogEventsAsync(It.IsAny<FilterLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NullReferenceException());

            testViewModel.ContinueLoadingCommand.Execute(null);

            Assert.Null(testViewModel.NextToken);
            Assert.Empty(testViewModel.LogEvents);

            Assert.Equal(2, paginatedLoadingStatus.Count);
            Assert.Equal(PaginatedLoadingStatus.Loading, paginatedLoadingStatus[0]);
            Assert.Equal(PaginatedLoadingStatus.None, paginatedLoadingStatus[1]);
            Assert.NotEmpty(testViewModel.ErrorMessage);
        }


        [Fact]
        public async Task RefreshAsync()
        {
            await SetupWithInitialLoad(SampleToken, SampleLogEvents);

            var newLogEvents = _eventsFixture.CreateSampleLogEvents();
            _eventsFixture.StubGetLogEventsToReturn("refresh-token", newLogEvents);

            await _viewModel.RefreshAsync();

            Assert.Equal("refresh-token", _viewModel.NextToken);
            Assert.Equal(newLogEvents, _viewModel.LogEvents);

            Assert.Equal(newLogEvents.First(), _viewModel.LogEvent);
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public async Task IsTimeFilterEnabled(bool isTimeFilterEnabled, int expectedTimesCalled)
        {
            _viewModel.DateTimeRange.StartDate = new DateTime(2022, 05, 04);
            _viewModel.DateTimeRange.EndDate = new DateTime(2022, 05, 05);

            _viewModel.IsTimeFilterEnabled = isTimeFilterEnabled;
            await _viewModel.LoadAsync();

            Repository.Verify(
                mock => mock.GetLogEventsAsync(It.Is<GetLogEventsRequest>(s => (s.EndTime!=null && s.StartTime!=null)), It.IsAny<CancellationToken>()),
                Times.Exactly(expectedTimesCalled));

        }

        /// <summary>
        /// Sets up an initial get load with the specified properties
        /// </summary>
        private async Task SetupWithInitialLoad(string token, List<LogEvent> logEvents)
        {
            _eventsFixture.StubGetLogEventsToReturn(token, logEvents);
            _eventsFixture.StubGetLogEventsToReturn(token, logEvents);
            await _viewModel.LoadAsync();
        }


        /// <summary>
        /// Sets up an initial filtered load with the specified properties
        /// </summary>
        private async Task SetupFilteredWithInitialLoad(string token, List<LogEvent> logEvents)
        {
            _eventsFixture.StubFilterLogEventsToReturn(token, logEvents);
            await _viewModel.LoadAsync();
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
