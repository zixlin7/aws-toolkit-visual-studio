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
    public class LogStreamsViewModelTests
    {
        private readonly LogStreamsViewModel _viewModel;

        private readonly LogStreamsFixture _streamsFixture = new LogStreamsFixture();
        private string SampleToken => _streamsFixture.SampleToken;
        private List<LogStream> SampleLogStreams => _streamsFixture.SampleLogStreams;
        private Mock<ICloudWatchLogsRepository> Repository => _streamsFixture.Repository;

        public LogStreamsViewModelTests()
        {
            _viewModel = _streamsFixture.CreateViewModel();
        }

        [Fact]
        public async Task LoadAsync_WhenInitial()
        {
            await _viewModel.LoadAsync();

            Assert.Equal(SampleToken, _viewModel.NextToken);
            Assert.Equal(SampleLogStreams, _viewModel.LogStreams);
            //first log stream is selected
            Assert.Equal(SampleLogStreams.First(), _viewModel.LogStream);
        }

        [Fact]
        public async Task LoadAsync_AdjustsLoadingLogs()
        {
            var loadingAdjustments = new List<bool>();

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
            var initialPageLogStreams = _streamsFixture.CreateSampleLogStreams();
            var expectedLogStreams = initialPageLogStreams.Concat(SampleLogStreams);

            await SetupWithInitialLoad("initial-token", initialPageLogStreams);

            _streamsFixture.StubGetLogStreamsToReturn(SampleToken, SampleLogStreams);

            await _viewModel.LoadAsync();

            Assert.Equal(SampleToken, _viewModel.NextToken);
            Assert.Equal(expectedLogStreams, _viewModel.LogStreams);
            //first log stream is selected
            Assert.Equal(expectedLogStreams.First(), _viewModel.LogStream);
        }


        [Fact]
        public async Task LoadAsync_WhenLastPage()
        {
            var initialPageLogStreams = _streamsFixture.CreateSampleLogStreams();
            var expectedLogStreams = initialPageLogStreams.Concat(SampleLogStreams);

            await SetupWithInitialLoad(SampleToken, initialPageLogStreams);

            _streamsFixture.StubGetLogStreamsToReturn(null, SampleLogStreams);

            await _viewModel.LoadAsync();

            Assert.Null(_viewModel.NextToken);
            Assert.Equal(expectedLogStreams, _viewModel.LogStreams);

            //first log stream is selected
            Assert.Equal(expectedLogStreams.First(), _viewModel.LogStream);

            Repository.Verify(
                mock => mock.GetLogStreamsAsync(It.IsAny<GetLogStreamsRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task LoadAsync_WhenNoMorePages()
        {
            await SetupWithInitialLoad(null, SampleLogStreams);

            _streamsFixture.StubGetLogStreamsToReturn(SampleToken, new List<LogStream>());

            await _viewModel.LoadAsync();

            Assert.Null(_viewModel.NextToken);
            Assert.Equal(SampleLogStreams, _viewModel.LogStreams);

            Repository.Verify(
                mock => mock.GetLogStreamsAsync(It.IsAny<GetLogStreamsRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }


        [Fact]
        public async Task LoadAsync_Throws()
        {
            Repository.Setup(mock =>
                    mock.GetLogStreamsAsync(It.IsAny<GetLogStreamsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NullReferenceException());

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await _viewModel.LoadAsync();
            });

            Assert.Empty(_viewModel.LogStreams);
            Assert.Null(_viewModel.NextToken);
        }

        [Fact]
        public async Task RefreshAsync()
        {
            await SetupWithInitialLoad(SampleToken, SampleLogStreams);

            var newLogStreams = _streamsFixture.CreateSampleLogStreams();
            _streamsFixture.StubGetLogStreamsToReturn("refresh-token", newLogStreams);

            await _viewModel.RefreshAsync();

            Assert.Equal("refresh-token", _viewModel.NextToken);
            Assert.Equal(newLogStreams, _viewModel.LogStreams);
            //first log stream is selected
            Assert.Equal(newLogStreams.First(), _viewModel.LogStream);
        }


        public static IEnumerable<object[]> FilterToOrderData = new List<object[]>
        {
            new object[] { null, OrderBy.LastEventTime, false },
            new object[] { "", OrderBy.LastEventTime, false },
            new object[] { "hello", OrderBy.LogStreamName, true }
        };

        [Theory]
        [MemberData(nameof(FilterToOrderData))]
        public void UpdateOrderBy(string filterText, OrderBy expectedOrder, bool expectedResult)
        {
            _viewModel.FilterText = filterText;

            var actualResult = _viewModel.UpdateOrderBy();

            Assert.Equal(expectedOrder, _viewModel.OrderBy);
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void UpdateIsDescendingToDefault(bool initialValue, bool expectedResult)
        {
            _viewModel.IsDescending = initialValue;

            var actualResult = _viewModel.UpdateIsDescendingToDefault();

            Assert.True(_viewModel.IsDescending);
            Assert.Equal(expectedResult, actualResult);
        }

        /// <summary>
        /// Sets up an initial load with the specified properties
        /// </summary>
        private async Task SetupWithInitialLoad(string token, List<LogStream> logStreams)
        {
            _streamsFixture.StubGetLogStreamsToReturn(token, logStreams);
            await _viewModel.LoadAsync();
        }
    }
}
