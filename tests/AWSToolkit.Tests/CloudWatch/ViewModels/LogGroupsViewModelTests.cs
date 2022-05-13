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
    public class LogGroupsViewModelTests
    {
        private readonly LogGroupsViewModel _viewModel;
        private readonly LogGroupsViewModelFixture _groupsFixture = new LogGroupsViewModelFixture();
        private string SampleToken => _groupsFixture.SampleToken;
        private List<LogGroup> SampleLogGroups => _groupsFixture.SampleLogGroups;
        private Mock<ICloudWatchLogsRepository> Repository => _groupsFixture.Repository;

        public LogGroupsViewModelTests()
        {
            _viewModel = _groupsFixture.CreateViewModel();
        }

        [Fact]
        public async Task LoadAsync_WhenInitial()
        {
            await _viewModel.LoadAsync();

            Assert.Equal(SampleToken, _viewModel.NextToken);
            Assert.Equal(SampleLogGroups, _viewModel.LogGroups);
            //first log group is selected
            Assert.Equal(SampleLogGroups.First(), _viewModel.LogGroup);
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
            var initialPageLogGroups = _groupsFixture.CreateSampleLogGroups();
            var expectedLogGroups = initialPageLogGroups.Concat(SampleLogGroups);

            await SetupWithInitialLoad("initial-token", initialPageLogGroups);

            _groupsFixture.StubGetLogGroupsToReturn(SampleToken, SampleLogGroups);

            await _viewModel.LoadAsync();

            Assert.Equal(SampleToken, _viewModel.NextToken);
            Assert.Equal(expectedLogGroups, _viewModel.LogGroups);
            //first log group is selected
            Assert.Equal(expectedLogGroups.First(), _viewModel.LogGroup);
        }

        [Fact]
        public async Task LoadAsync_WhenLastPage()
        {
            var initialPageLogGroups = _groupsFixture.CreateSampleLogGroups();
            var expectedLogGroups = initialPageLogGroups.Concat(SampleLogGroups);

            await SetupWithInitialLoad(SampleToken, initialPageLogGroups);

            _groupsFixture.StubGetLogGroupsToReturn(null, SampleLogGroups);

            await _viewModel.LoadAsync();

            Assert.Null(_viewModel.NextToken);
            Assert.Equal(expectedLogGroups, _viewModel.LogGroups);

            //first log group is selected
            Assert.Equal(expectedLogGroups.First(), _viewModel.LogGroup);

            Repository.Verify(
                mock => mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task LoadAsync_WhenNoMorePages()
        {
            await SetupWithInitialLoad(null, SampleLogGroups);

            _groupsFixture.StubGetLogGroupsToReturn(SampleToken, new List<LogGroup>());

            await _viewModel.LoadAsync();

            Assert.Null(_viewModel.NextToken);
            Assert.Equal(SampleLogGroups, _viewModel.LogGroups);

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
                await _viewModel.LoadAsync();
            });

            Assert.Empty(_viewModel.LogGroups);
            Assert.Null(_viewModel.NextToken);
        }

        [Fact]
        public async Task RefreshAsync()
        {
            await SetupWithInitialLoad(SampleToken, SampleLogGroups);

            var newLogGroups = _groupsFixture.CreateSampleLogGroups();
            _groupsFixture.StubGetLogGroupsToReturn("refresh-token", newLogGroups);

            await _viewModel.RefreshAsync();

            Assert.Equal("refresh-token", _viewModel.NextToken);
            Assert.Equal(newLogGroups, _viewModel.LogGroups);
            //first log group is selected
            Assert.Equal(newLogGroups.First(), _viewModel.LogGroup);
        }

        /// <summary>
        /// Sets up an initial load with the specified properties
        /// </summary>
        private async Task SetupWithInitialLoad(string token, List<LogGroup> logGroups)
        {
            _groupsFixture.StubGetLogGroupsToReturn(token, logGroups);
            await _viewModel.LoadAsync();
        }
    }
}
