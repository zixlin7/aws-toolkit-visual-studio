using System;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.CloudWatch.Views;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Commands
{
    /// <summary>
    /// Command that creates/displays a log groups explorer in a tool window
    /// </summary>
    public class ViewLogGroupsCommand : BaseContextCommand
    {
        static ILog Logger = LogManager.GetLogger(typeof(ViewLogGroupsCommand));

        private readonly ToolkitContext _toolkitContext;
        private LogGroupsRootViewModel _rootModel;

        public ViewLogGroupsCommand(ToolkitContext context)
        {
            _toolkitContext = context;
        }

        public override ActionResults Execute(IViewModel model)
        {
            _rootModel = model as LogGroupsRootViewModel;
            if (_rootModel == null)
            {
                return new ActionResults().WithSuccess(false);
            }

            try
            {
                CreateLogGroupsViewer();
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing log groups", ex);
                return new ActionResults().WithSuccess(false);
            }

            return new ActionResults().WithSuccess(true);
        }

        private void CreateLogGroupsViewer()
        {
            var toolWindowFactory = _toolkitContext.ToolkitHost.GetToolWindowFactory();
            var logGroupsModel = CreateLogGroupsViewModel();
            var newControl = new LogGroupsViewerControl { DataContext = logGroupsModel };

            bool ConnectionSettingsFunc(BaseAWSControl awsControl)
            {
                var currentControl = awsControl as LogGroupsViewerControl;
                return currentControl?.ConnectionSettings?.CredentialIdentifier?.Id !=
                       newControl.ConnectionSettings?.CredentialIdentifier?.Id ||
                       currentControl?.ConnectionSettings?.Region?.Id != newControl.ConnectionSettings?.Region?.Id;
            }

            toolWindowFactory.ShowLogGroupsToolWindow(newControl, ConnectionSettingsFunc);
        }

        private LogGroupsViewModel CreateLogGroupsViewModel()
        {
            var repositoryFactory = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)) as IRepositoryFactory;
            if (repositoryFactory == null)
            {
                throw new Exception("Unable to load CloudWatch log groups data source");
            }

            var awsConnectionSetting = new AwsConnectionSettings(_rootModel.AccountViewModel?.Identifier, _rootModel.Region);
            var cwLogsRepository =
                repositoryFactory.CreateCloudWatchLogsRepository(awsConnectionSetting);

            var viewModel = new LogGroupsViewModel(cwLogsRepository, _toolkitContext);
            viewModel.RefreshCommand = RefreshLogsCommand.Create(viewModel);
            viewModel.ViewCommand = ViewLogStreamsCommand.Create(_toolkitContext, cwLogsRepository.ConnectionSettings);
            return viewModel;
        }
    }
}
