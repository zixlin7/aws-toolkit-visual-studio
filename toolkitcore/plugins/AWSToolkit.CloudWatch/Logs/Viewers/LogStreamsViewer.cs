using System;

using Amazon.AWSToolkit.CloudWatch.Logs.Commands;
using Amazon.AWSToolkit.CloudWatch.Logs.Core;
using Amazon.AWSToolkit.CloudWatch.Logs.ViewModels;
using Amazon.AWSToolkit.CloudWatch.Logs.Views;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CloudWatch.Logs.Viewers
{
    public class LogStreamsViewer : ILogStreamsViewer
    {
        private readonly ToolkitContext _toolkitContext;

        public LogStreamsViewer(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public BaseAWSControl GetViewer(string logGroup, AwsConnectionSettings connectionSettings)
        {
            var viewModel = CreateLogStreamsViewModel(connectionSettings);
            viewModel.LogGroup = logGroup;
            var control = new LogStreamsViewerControl { DataContext = viewModel };
            return control;
        }

        public void View(string logGroup, AwsConnectionSettings connectionSettings)
        {
            var control = GetViewer(logGroup, connectionSettings);
            _toolkitContext.ToolkitHost.OpenInEditor(control);
        }

        private LogStreamsViewModel CreateLogStreamsViewModel(
            AwsConnectionSettings connectionSettings)
        {
            var repositoryFactory =
                _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)) as
                    IRepositoryFactory;
            if (repositoryFactory == null)
            {
                throw new Exception("Unable to load CloudWatch log streams data source");
            }

            var cwLogsRepository =
                repositoryFactory.CreateCloudWatchLogsRepository(connectionSettings);

            var viewModel = new LogStreamsViewModel(cwLogsRepository, _toolkitContext);
            viewModel.RefreshCommand = RefreshLogsCommand.Create(viewModel);
            viewModel.ViewCommand = ViewLogEventsCommand.Create(_toolkitContext, cwLogsRepository.ConnectionSettings);
            viewModel.ExportStreamCommand = ExportStreamCommand.Create(cwLogsRepository, _toolkitContext);
            return viewModel;
        }
    }
}
