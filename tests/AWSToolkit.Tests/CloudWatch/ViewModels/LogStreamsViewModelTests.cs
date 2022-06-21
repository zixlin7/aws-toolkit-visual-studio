using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;

using AWSToolkit.Tests.CloudWatch.Fixtures;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.ViewModels
{
    public class LogStreamsViewModelTests : BaseLogEntityViewModelTests<LogStreamsViewModel>
    {
        private readonly LogStreamsFixture _streamsFixture = new LogStreamsFixture();

        public LogStreamsViewModelTests()
        {
            ViewModelFixture = _streamsFixture;
            ViewModel = _streamsFixture.CreateViewModel();
        }

        [Fact]
        public async Task LoadAsync_WhenInitial()
        {
            await ViewModel.LoadAsync();

            Assert.Equal(SampleToken, ViewModel.NextToken);
            Assert.Equal(SampleLogStreams, ViewModel.LogStreams);
            //first log stream is selected
            Assert.Equal(SampleLogStreams.First(), ViewModel.LogStream);
        }

        [Fact]
        public async Task LoadAsync_WhenMorePages()
        {
            var initialPageLogStreams = _streamsFixture.CreateSampleLogStreams();
            var expectedLogStreams = initialPageLogStreams.Concat(SampleLogStreams);

            await SetupWithInitialLoad("initial-token", initialPageLogStreams);

            _streamsFixture.StubGetLogStreamsToReturn(SampleToken, SampleLogStreams);

            await ViewModel.LoadAsync();

            Assert.Equal(SampleToken, ViewModel.NextToken);
            Assert.Equal(expectedLogStreams, ViewModel.LogStreams);
            //first log stream is selected
            Assert.Equal(expectedLogStreams.First(), ViewModel.LogStream);
        }


        [Fact]
        public async Task LoadAsync_WhenLastPage()
        {
            var initialPageLogStreams = _streamsFixture.CreateSampleLogStreams();
            var expectedLogStreams = initialPageLogStreams.Concat(SampleLogStreams);

            await SetupWithInitialLoad(SampleToken, initialPageLogStreams);

            _streamsFixture.StubGetLogStreamsToReturn(null, SampleLogStreams);

            await ViewModel.LoadAsync();

            Assert.Null(ViewModel.NextToken);
            Assert.Equal(expectedLogStreams, ViewModel.LogStreams);

            //first log stream is selected
            Assert.Equal(expectedLogStreams.First(), ViewModel.LogStream);

            Repository.Verify(
                mock => mock.GetLogStreamsAsync(It.IsAny<GetLogStreamsRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task LoadAsync_WhenNoMorePages()
        {
            await SetupWithInitialLoad(null, SampleLogStreams);

            _streamsFixture.StubGetLogStreamsToReturn(SampleToken, new List<LogStream>());

            await ViewModel.LoadAsync();

            Assert.Null(ViewModel.NextToken);
            Assert.Equal(SampleLogStreams, ViewModel.LogStreams);

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
                await ViewModel.LoadAsync();
            });

            Assert.Empty(ViewModel.LogStreams);
            Assert.Null(ViewModel.NextToken);
        }

        [Fact]
        public async Task RefreshAsync()
        {
            await SetupWithInitialLoad(SampleToken, SampleLogStreams);

            var newLogStreams = _streamsFixture.CreateSampleLogStreams();
            _streamsFixture.StubGetLogStreamsToReturn("refresh-token", newLogStreams);

            await ViewModel.RefreshAsync();

            Assert.Equal("refresh-token", ViewModel.NextToken);
            Assert.Equal(newLogStreams, ViewModel.LogStreams);
            //first log stream is selected
            Assert.Equal(newLogStreams.First(), ViewModel.LogStream);
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
            ViewModel.FilterText = filterText;

            var actualResult = ViewModel.UpdateOrderBy();

            Assert.Equal(expectedOrder, ViewModel.OrderBy);
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void UpdateIsDescendingToDefault(bool initialValue, bool expectedResult)
        {
            ViewModel.IsDescending = initialValue;

            var actualResult = ViewModel.UpdateIsDescendingToDefault();

            Assert.True(ViewModel.IsDescending);
            Assert.Equal(expectedResult, actualResult);
        }

        /// <summary>
        /// Sets up an initial load with the specified properties
        /// </summary>
        private async Task SetupWithInitialLoad(string token, List<LogStream> logStreams)
        {
            _streamsFixture.StubGetLogStreamsToReturn(token, logStreams);
            await ViewModel.LoadAsync();
        }
    }
}
