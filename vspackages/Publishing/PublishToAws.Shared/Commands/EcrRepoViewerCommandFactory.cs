using System;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Ecr;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;

using log4net;

namespace Amazon.AWSToolkit.Publish.Commands
{
    /// <summary>
    /// Creates a WPF command that opens an ECR Repository for viewing
    /// </summary>
    public class EcrRepoViewerCommandFactory
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(EcrRepoViewerCommandFactory));

        public static ICommand Create(PublishToAwsDocumentViewModel viewModel)
        {
            return new RelayCommand(_ => CanView(viewModel), _ => ViewRepository(viewModel));
        }

        private static bool CanView(PublishToAwsDocumentViewModel viewModel)
        {
            return viewModel.PublishDestination?.DeploymentArtifact == DeploymentArtifact.ElasticContainerRegistry;
        }

        private static void ViewRepository(PublishToAwsDocumentViewModel viewModel)
        {
            string artifactId = string.Empty;

            try
            {
                artifactId = viewModel.PublishProjectViewModel.PublishedArtifactId;

                if (string.IsNullOrWhiteSpace(artifactId))
                {
                    throw new ArgumentException("No ECR Repository name");
                }

                var publishContext = viewModel.PublishContext;

                var repoViewer =
                    publishContext.ToolkitShellProvider.QueryAWSToolkitPluginService(typeof(IEcrViewer)) as IEcrViewer;

                if (repoViewer == null)
                {
                    throw new Exception("Toolkit was unable to access the ECR Repo viewer.");
                }

                repoViewer.ViewRepository(artifactId,
                    new AwsConnectionSettings(
                        publishContext.ConnectionManager.ActiveCredentialIdentifier,
                        publishContext.ConnectionManager.ActiveRegion
                    )
                );
            }
            catch (Exception e)
            {
                Logger.Error($"Failure to view the ECR Repo {artifactId}", e);
                viewModel.PublishContext.ToolkitShellProvider.OutputToHostConsole(
                    $"Unable to view ECR Repository {artifactId}: {e.Message}",
                    true);
            }
        }
    }
}
