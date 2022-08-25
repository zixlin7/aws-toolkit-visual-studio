using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Logs.Models;
using Amazon.AWSToolkit.CloudWatch.Logs.ViewModels;

using AWSToolkit.Tests.CloudWatch.Logs.Fixtures;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.Logs.ViewModels
{
    public class LogGroupsViewModelTests : BaseLogEntityViewModelTests<LogGroupsViewModel>
    {
        private readonly LogGroupsViewModelFixture _groupsFixture = new LogGroupsViewModelFixture();

        public LogGroupsViewModelTests()
        {
            ViewModelFixture = _groupsFixture;
            ViewModel = _groupsFixture.CreateViewModel();
        }

        [Fact]
        public async Task LoadAsync_WhenInitial()
        {
            await ViewModel.LoadAsync();

            Assert.Equal(SampleToken, ViewModel.NextToken);
            Assert.Equal(SampleLogGroups, ViewModel.LogGroups);
            //first log group is selected
            Assert.Equal(SampleLogGroups.First(), ViewModel.LogGroup);
        }

        [Fact]
        public async Task LoadAsync_WhenMorePages()
        {
            var initialPageLogGroups = _groupsFixture.CreateSampleLogGroups();
            var expectedLogGroups = initialPageLogGroups.Concat(SampleLogGroups);

            await SetupWithInitialLoad("initial-token", initialPageLogGroups);

            _groupsFixture.StubGetLogGroupsToReturn(SampleToken, SampleLogGroups);

            await ViewModel.LoadAsync();

            Assert.Equal(SampleToken, ViewModel.NextToken);
            Assert.Equal(expectedLogGroups, ViewModel.LogGroups);
            //first log group is selected
            Assert.Equal(expectedLogGroups.First(), ViewModel.LogGroup);
        }

        [Fact]
        public async Task LoadAsync_WhenLastPage()
        {
            var initialPageLogGroups = _groupsFixture.CreateSampleLogGroups();
            var expectedLogGroups = initialPageLogGroups.Concat(SampleLogGroups);

            await SetupWithInitialLoad(SampleToken, initialPageLogGroups);

            _groupsFixture.StubGetLogGroupsToReturn(null, SampleLogGroups);

            await ViewModel.LoadAsync();

            Assert.Null(ViewModel.NextToken);
            Assert.Equal(expectedLogGroups, ViewModel.LogGroups);

            //first log group is selected
            Assert.Equal(expectedLogGroups.First(), ViewModel.LogGroup);

            Repository.Verify(
                mock => mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task LoadAsync_WhenNoMorePages()
        {
            await SetupWithInitialLoad(null, SampleLogGroups);

            _groupsFixture.StubGetLogGroupsToReturn(SampleToken, new List<LogGroup>());

            await ViewModel.LoadAsync();

            Assert.Null(ViewModel.NextToken);
            Assert.Equal(SampleLogGroups, ViewModel.LogGroups);

            Repository.Verify(
                mock => mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }


        [Fact]
        public async Task LoadAsync_Throws()
        {
            Repository.Setup(mock =>
                    mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NullReferenceException());

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await ViewModel.LoadAsync();
            });

            Assert.Empty(ViewModel.LogGroups);
            Assert.Null(ViewModel.NextToken);
        }


        [Fact]
        public async Task RefreshAsync()
        {
            await SetupWithInitialLoad(SampleToken, SampleLogGroups);

            var newLogGroups = _groupsFixture.CreateSampleLogGroups();
            _groupsFixture.StubGetLogGroupsToReturn("refresh-token", newLogGroups);

            await ViewModel.RefreshAsync();

            Assert.Equal("refresh-token", ViewModel.NextToken);
            Assert.Equal(newLogGroups, ViewModel.LogGroups);
            //first log group is selected
            Assert.Equal(newLogGroups.First(), ViewModel.LogGroup);
        }

        /// <summary>
        /// Sets up an initial load with the specified properties
        /// </summary>
        private async Task SetupWithInitialLoad(string token, List<LogGroup> logGroups)
        {
            _groupsFixture.StubGetLogGroupsToReturn(token, logGroups);
            await ViewModel.LoadAsync();
        }
    }
}
