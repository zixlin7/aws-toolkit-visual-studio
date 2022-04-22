using System;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.CloudWatch.Views;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Commands
{
    public class ViewLogStreamsCommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ViewLogStreamsCommand));

        public static ICommand Create(ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings)
        {
            return new RelayCommand(parameter => ViewLogStreams(toolkitContext, connectionSettings, parameter));
        }

        private static void ViewLogStreams(ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings, object parameter)
        {
            try 
            {
                if (!(parameter is LogGroup logGroup))
                {
                    throw new ArgumentException($"Parameter is not of expected type: {typeof(LogGroup)}");
                }

                var viewModel = CreateLogStreamsViewModel(toolkitContext, connectionSettings);
                viewModel.LogGroup = logGroup;

                var control = new LogStreamsViewerControl{ DataContext = viewModel };
                toolkitContext.ToolkitHost.OpenInEditor(control);
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing log streams", ex);
                toolkitContext.ToolkitHost.ShowError($"Error viewing log streams: {ex.Message}");
            }
        }

        private static LogStreamsViewModel CreateLogStreamsViewModel(ToolkitContext toolkitContext,
            AwsConnectionSettings connectionSettings)
        {
            var repositoryFactory =
                toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)) as
                    IRepositoryFactory;
            if (repositoryFactory == null)
            {
                throw new Exception("Unable to load CloudWatch log streams data source");
            }

            var cwLogsRepository =
                repositoryFactory.CreateCloudWatchLogsRepository(connectionSettings);

            var viewModel = new LogStreamsViewModel(cwLogsRepository, toolkitContext);
            viewModel.RefreshCommand = RefreshLogsCommand.Create(viewModel);
            return viewModel;
        }
    }
}
