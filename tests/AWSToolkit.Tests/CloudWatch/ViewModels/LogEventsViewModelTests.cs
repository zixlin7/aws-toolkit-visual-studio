using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;

using AWSToolkit.Tests.CloudWatch.Fixtures;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.ViewModels
{
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

            await SetupWithInitialLoad(SampleToken, initialPageLogEvents);

            _eventsFixture.StubGetLogEventsToReturn(null, SampleLogEvents);

            await _viewModel.LoadAsync();

            Assert.Null(_viewModel.NextToken);
            Assert.Equal(expectedLogEvents, _viewModel.LogEvents);
            Assert.Equal(expectedLogEvents.First(), _viewModel.LogEvent);

            Repository.Verify(
                mock => mock.GetLogEventsAsync(It.IsAny<GetLogEventsRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task LoadAsync_WhenNoMorePages()
        {
            await SetupWithInitialLoad(null, SampleLogEvents);

            _eventsFixture.StubGetLogEventsToReturn(SampleToken, new List<LogEvent>());

            await _viewModel.LoadAsync();

            Assert.Null(_viewModel.NextToken);
            Assert.Equal(SampleLogEvents, _viewModel.LogEvents);

            Repository.Verify(
                mock => mock.GetLogEventsAsync(It.IsAny<GetLogEventsRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
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

        /// <summary>
        /// Sets up an initial load with the specified properties
        /// </summary>
        private async Task SetupWithInitialLoad(string token, List<LogEvent> logEvents)
        {
            _eventsFixture.StubGetLogEventsToReturn(token, logEvents);
            await _viewModel.LoadAsync();
        }
    }
}
