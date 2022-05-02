using System;
using System.Linq;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.CloudWatch.Views;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Commands
{
    public class ViewLogEventsCommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ViewLogEventsCommand));

        public static ICommand Create(ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings)
        {
            return new RelayCommand(parameter => ViewLogEvents(parameter, toolkitContext, connectionSettings));
        }

        private static void ViewLogEvents(object parameter, ToolkitContext toolkitContext,
            AwsConnectionSettings connectionSettings)
        {
            try
            {
                var parameters = (object[]) parameter;
                if (parameters == null || parameters.Count() != 2)
                {
                    throw new ArgumentException($"Expected parameters: 2, Found: {parameters?.Count()}");
                }

                if (parameters.Any(x => x.GetType() != typeof(string)))
                {
                    throw new ArgumentException($"Parameters are not of expected type: {typeof(string)}");
                }

                var viewModel = CreateLogEventsViewModel(toolkitContext, connectionSettings);
                viewModel.LogGroup = parameters[0] as string;
                viewModel.LogStream = parameters[1] as string;

                var control = new LogEventsViewerControl { DataContext = viewModel };
                toolkitContext.ToolkitHost.OpenInEditor(control);
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing log events", ex);
                toolkitContext.ToolkitHost.ShowError($"Error viewing log events: {ex.Message}");
            }
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
            return viewModel;
        }
    }
}
