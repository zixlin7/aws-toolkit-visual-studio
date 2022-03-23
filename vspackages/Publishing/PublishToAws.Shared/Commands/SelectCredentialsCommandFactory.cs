using System;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialSelector;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Shared;

using log4net;

namespace Amazon.AWSToolkit.Publish.Commands
{
    public class SelectCredentialsCommandFactory
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SelectCredentialsCommandFactory));

        private SelectCredentialsCommandFactory()
        {
        }

        public static ICommand Create(PublishToAwsDocumentViewModel viewModel)
        {
            return new RelayCommand(_ => CanExecute(viewModel), _ => Execute(viewModel));
        }

        private static bool CanExecute(PublishToAwsDocumentViewModel viewModel)
        {
            if (viewModel.IsPublishing || viewModel.ViewStage != PublishViewStage.Target) { return false; }

            return true;
        }

        private static void Execute(PublishToAwsDocumentViewModel viewModel)
        {
            try
            {
                using (var dialog = CreateDialog(viewModel.PublishContext.ToolkitShellProvider))
                {
                    dialog.IncludeLocalRegions = false;
                    dialog.CredentialIdentifier = viewModel.Connection.CredentialsId;
                    dialog.Region = viewModel.Connection.Region;

                    if (!dialog.Show()) { return; }

                    viewModel.PublishContext.ConnectionManager.ChangeConnectionSettings(
                        dialog.CredentialIdentifier, dialog.Region);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failure while choosing credentials and region.", e);
                viewModel.PublishContext.ToolkitShellProvider.OutputToHostConsole(
                    $"Error adjusting the Publish to AWS connection: {e.Message}", true);
            }
        }

        private static ICredentialSelectionDialog CreateDialog(IAWSToolkitShellProvider toolkitHost)
        {
            return toolkitHost.GetDialogFactory().CreateCredentialsSelectionDialog();
        }
    }
}
