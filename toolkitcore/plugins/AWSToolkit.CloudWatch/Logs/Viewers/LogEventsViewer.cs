using System;

using Amazon.AWSToolkit.CloudWatch.Logs.Commands;
using Amazon.AWSToolkit.CloudWatch.Logs.Core;
using Amazon.AWSToolkit.CloudWatch.Logs.ViewModels;
using Amazon.AWSToolkit.CloudWatch.Logs.Views;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CloudWatch.Logs.Viewers
{
    public class LogEventsViewer : ILogEventsViewer
    {
        private readonly ToolkitContext _toolkitContext;

        public LogEventsViewer(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public void View(string logGroup, string logStream, AwsConnectionSettings connectionSettings)
        {
            var viewModel = CreateLogEventsViewModel(_toolkitContext, connectionSettings);
            viewModel.LogGroup = logGroup;
            viewModel.LogStream = logStream;

            var control = new LogEventsViewerControl { DataContext = viewModel };
            _toolkitContext.ToolkitHost.OpenInEditor(control);
        }

        private static LogEventsViewModel CreateLogEventsViewModel(ToolkitContext toolkitContext,
            AwsConnectionSettings connectionSettings)
        {
            var repositoryFactory =
                toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)) as
                    IRepositoryFactory;
            if (repositoryFactory == null)
            {
                throw new Exception("Unable to load CloudWatch log events data source");
            }

            var cwLogsRepository =
                repositoryFactory.CreateCloudWatchLogsRepository(connectionSettings);

            var viewModel = new LogEventsViewModel(cwLogsRepository, toolkitContext);
            viewModel.RefreshCommand = RefreshLogsCommand.Create(viewModel);
            return viewModel;
        }
    }
}
