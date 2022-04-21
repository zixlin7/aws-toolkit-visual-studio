using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch
{
    public class LogStreamsViewModelTests
    {
        private readonly LogStreamsViewModel _viewModel;
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<ICloudWatchLogsRepository> _repository = new Mock<ICloudWatchLogsRepository>();
        private readonly string _sampleToken = "sample-token";
        private readonly LogGroup _sampleLogGroup = new LogGroup() { Name = "lg", Arn = "lg-arn" };
        private readonly List<LogStream> _sampleLogStreams;

        public LogStreamsViewModelTests()
        {
            _contextFixture.SetupExecuteOnUIThread();
            _sampleLogStreams = CreateSampleLogStreams();
            StubGetLogStreamsToReturn(_sampleToken, _sampleLogStreams);
            _viewModel = new LogStreamsViewModel(_repository.Object, _contextFixture.ToolkitContext)
            {
                LogGroup = _sampleLogGroup
            };
        }

        [Fact]
        public async Task LoadAsync_WhenInitial()
        {
            await _viewModel.LoadAsync();

            Assert.Equal(_sampleToken, _viewModel.NextToken);
            Assert.Equal(_sampleLogStreams, _viewModel.LogStreams);
            //first log stream is selected
            Assert.Equal(_sampleLogStreams.First(), _viewModel.LogStream);
        }

        [Fact]
        public async Task LoadAsync_WhenMorePages()
        {
            var initialPageLogStreams = CreateSampleLogStreams();
            var expectedLogStreams = initialPageLogStreams.Concat(_sampleLogStreams);

            await SetupWithInitialLoad("initial-token", initialPageLogStreams);

            StubGetLogStreamsToReturn(_sampleToken, _sampleLogStreams);

            await _viewModel.LoadAsync();

            Assert.Equal(_sampleToken, _viewModel.NextToken);
            Assert.Equal(expectedLogStreams, _viewModel.LogStreams);
            //first log stream is selected
            Assert.Equal(expectedLogStreams.First(), _viewModel.LogStream);
        }


        [Fact]
        public async Task LoadAsync_WhenLastPage()
        {
            var initialPageLogStreams = CreateSampleLogStreams();
            var expectedLogStreams = initialPageLogStreams.Concat(_sampleLogStreams);

            await SetupWithInitialLoad(_sampleToken, initialPageLogStreams);

            StubGetLogStreamsToReturn(null, _sampleLogStreams);

            await _viewModel.LoadAsync();

            Assert.Null(_viewModel.NextToken);
            Assert.Equal(expectedLogStreams, _viewModel.LogStreams);

            //first log stream is selected
            Assert.Equal(expectedLogStreams.First(), _viewModel.LogStream);

            _repository.Verify(
                mock => mock.GetLogStreamsAsync(It.IsAny<GetLogStreamsRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task LoadAsync_WhenNoMorePages()
        {
            await SetupWithInitialLoad(null, _sampleLogStreams);

            StubGetLogStreamsToReturn(_sampleToken, new List<LogStream>());

            await _viewModel.LoadAsync();

            Assert.Null(_viewModel.NextToken);
            Assert.Equal(_sampleLogStreams, _viewModel.LogStreams);

            _repository.Verify(
                mock => mock.GetLogStreamsAsync(It.IsAny<GetLogStreamsRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }


        [Fact]
        public async Task LoadAsync_Throws()
        {
            _repository.Setup(mock =>
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
            await SetupWithInitialLoad(_sampleToken, _sampleLogStreams);

            var newLogStreams = CreateSampleLogStreams();
            StubGetLogStreamsToReturn("refresh-token", newLogStreams);

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

        private List<LogStream> CreateSampleLogStreams()
        {
            return Enumerable.Range(1, 3).Select(i =>
            {
                var guid = Guid.NewGuid().ToString();
                return new LogStream() { Name = $"lg-{guid}", Arn = $"lg-{guid}-arn", LastEventTime = DateTime.Now };
            }).ToList();
        }

        private void StubGetLogStreamsToReturn(string nextToken, List<LogStream> logStreams)
        {
            var response = new PaginatedLogResponse<LogStream>(nextToken, logStreams);
            _repository.Setup(mock =>
                    mock.GetLogStreamsAsync(It.IsAny<GetLogStreamsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }

        /// <summary>
        /// Sets up an initial load with the specified properties
        /// </summary>
        private async Task SetupWithInitialLoad(string token, List<LogStream> logStreams)
        {
            StubGetLogStreamsToReturn(token, logStreams);
            await _viewModel.LoadAsync();
        }
    }
}
