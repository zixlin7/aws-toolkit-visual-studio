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
    public class LogGroupsViewModelTests
    {
        private readonly LogGroupsViewModel _viewModel;
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<ICloudWatchLogsRepository> _repository = new Mock<ICloudWatchLogsRepository>();
        private readonly string _sampleToken = "sample-token";
        private readonly List<LogGroup> _sampleLogGroups;

        public LogGroupsViewModelTests()
        {
            _contextFixture.SetupExecuteOnUIThread();
            _sampleLogGroups = CreateSampleLogGroups();
            StubGetLogGroupsToReturn(_sampleToken, _sampleLogGroups);
            _viewModel = new LogGroupsViewModel(_repository.Object, _contextFixture.ToolkitContext);
        }

        [Fact]
        public async Task LoadAsync_WhenInitial()
        {
            await _viewModel.LoadAsync();

            Assert.Equal(_sampleToken, _viewModel.NextToken);
            Assert.Equal(_sampleLogGroups, _viewModel.LogGroups);
            //first log group is selected
            Assert.Equal(_sampleLogGroups.First(), _viewModel.LogGroup);
        }

        [Fact]
        public async Task LoadAsync_WhenMorePages()
        {
            var expectedLogGroups = CreateSampleLogGroups();
            await SetupWithInitialLoad("initial-token", expectedLogGroups);

            StubGetLogGroupsToReturn(_sampleToken, _sampleLogGroups);

            await _viewModel.LoadAsync();

            expectedLogGroups.AddRange(_sampleLogGroups);

            Assert.Equal(_sampleToken, _viewModel.NextToken);
            Assert.Equal(expectedLogGroups, _viewModel.LogGroups);
            //first log group is selected
            Assert.Equal(expectedLogGroups.First(), _viewModel.LogGroup);
        }

        [Fact]
        public async Task LoadAsync_WhenLastPage()
        {
            var expectedLogGroups = CreateSampleLogGroups();
            await SetupWithInitialLoad(_sampleToken, expectedLogGroups);

            StubGetLogGroupsToReturn(null, _sampleLogGroups);

            await _viewModel.LoadAsync();

            expectedLogGroups.AddRange(_sampleLogGroups);

            Assert.Null(_viewModel.NextToken);
            Assert.Equal(expectedLogGroups, _viewModel.LogGroups);

            //first log group is selected
            Assert.Equal(expectedLogGroups.First(), _viewModel.LogGroup);

            _repository.Verify(mock => mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task LoadAsync_WhenNoMorePages()
        {
            await SetupWithInitialLoad(null, _sampleLogGroups);

            StubGetLogGroupsToReturn(_sampleToken, new List<LogGroup>());

            await _viewModel.LoadAsync();

            Assert.Null(_viewModel.NextToken);
            Assert.Equal(_sampleLogGroups, _viewModel.LogGroups);

            _repository.Verify(mock => mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task LoadAsync_Throws()
        {
            _repository.Setup(mock =>
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
            await SetupWithInitialLoad(_sampleToken, _sampleLogGroups);

            var newLogGroups = CreateSampleLogGroups();
            StubGetLogGroupsToReturn("refresh-token", newLogGroups);

            await _viewModel.RefreshAsync();

            Assert.Equal("refresh-token", _viewModel.NextToken);
            Assert.Equal(newLogGroups, _viewModel.LogGroups);
            //first log group is selected
            Assert.Equal(newLogGroups.First(), _viewModel.LogGroup);
        }

        private List<LogGroup> CreateSampleLogGroups()
        {
            return Enumerable.Range(1, 3).Select(i =>
            {
                var guid = Guid.NewGuid().ToString();
                return new LogGroup() { Name = $"lg-{guid}", Arn = $"lg-{guid}-arn" };
            }).ToList();
        }

        private void StubGetLogGroupsToReturn(string nextToken, List<LogGroup> logGroups)
        {
            var response = new PaginatedLogResponse<LogGroup>(nextToken, logGroups);
            _repository.Setup(mock =>
                    mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }

        /// <summary>
        /// Sets up an initial load with the specified properties
        /// </summary>
        private async Task SetupWithInitialLoad(string token, List<LogGroup> logGroups)
        {
            StubGetLogGroupsToReturn(token, logGroups);
            await _viewModel.LoadAsync();
        }
    }
}
