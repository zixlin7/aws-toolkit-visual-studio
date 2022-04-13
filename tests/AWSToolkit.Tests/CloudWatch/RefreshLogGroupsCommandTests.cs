using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Commands;
using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch
{
    public class RefreshLogGroupsCommandTests
    {
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<ICloudWatchLogsRepository> _repository = new Mock<ICloudWatchLogsRepository>();

        private readonly LogGroupsViewModel _viewModel;
        private readonly IList<LogGroup> _sampleLogGroups;
        private readonly ICommand _command;
        
        public RefreshLogGroupsCommandTests()
        {
            _sampleLogGroups = CreateSampleLogGroups();
            SetupGetLogGroups();
            _contextFixture.SetupExecuteOnUIThread();

            _viewModel = new LogGroupsViewModel(_repository.Object, _contextFixture.ToolkitContext);
            _command = RefreshLogGroupsCommand.Create(_viewModel);
        }

        [Fact]
        public void Execute()
        {
            _viewModel.NextToken = "initial-token";

            _command.Execute(null);

            Assert.Equal("sample-token", _viewModel.NextToken);
            Assert.Equal(_sampleLogGroups, _viewModel.LogGroups);
            Assert.Empty(_viewModel.ErrorMessage);
        }

        [Fact]
        public void Execute_WhenError()
        {
            _repository.Setup(mock =>
                    mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NullReferenceException("null reference found"));

            _command.Execute(null);

            Assert.Contains("null reference found", _viewModel.ErrorMessage);
        }

        private List<LogGroup> CreateSampleLogGroups()
        {
            return Enumerable.Range(1, 3).Select(i =>
            {
                var guid = Guid.NewGuid().ToString();
                return new LogGroup() { Name = $"lg-{guid}", Arn = $"lg-{guid}-arn" };
            }).ToList();
        }

        private void SetupGetLogGroups()
        {
            var response = new PaginatedLogResponse<LogGroup>("sample-token", _sampleLogGroups);
            _repository.Setup(mock =>
                    mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }
    }
}
