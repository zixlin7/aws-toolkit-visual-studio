using System;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudFormation;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;

using log4net;

namespace Amazon.AWSToolkit.Publish.Commands
{
    /// <summary>
    /// Creates a WPF command that opens a CloudFormation stack for viewing
    /// </summary>
    public class StackViewerCommandFactory
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(StackViewerCommandFactory));

        public static ICommand Create(PublishToAwsDocumentViewModel viewModel)
        {
            return new RelayCommand(_ => CanView(viewModel), (obj) => ViewStack(viewModel));
        }

        private static bool CanView(PublishToAwsDocumentViewModel viewModel)
        {
            return viewModel.PublishDestination?.DeploymentArtifact == DeploymentArtifact.CloudFormationStack;
        }

        private static void ViewStack(PublishToAwsDocumentViewModel viewModel)
        {
            string artifactId = string.Empty;

            try
            {
                artifactId = viewModel.PublishProjectViewModel.PublishedArtifactId;
                var publishContext = viewModel.PublishContext;

                var cloudFormationViewer =
                    publishContext.ToolkitShellProvider.QueryAWSToolkitPluginService(typeof(ICloudFormationViewer)) as
                        ICloudFormationViewer;

                cloudFormationViewer.View(artifactId,
                    new AwsConnectionSettings(
                        publishContext.ConnectionManager.ActiveCredentialIdentifier,
                        publishContext.ConnectionManager.ActiveRegion
                    ));
            }
            catch (Exception e)
            {
                viewModel.PublishContext.ToolkitShellProvider.OutputError(new Exception($"Error viewing CloudFormation stack {artifactId}", e), Logger);
            }
        }
    }
}
