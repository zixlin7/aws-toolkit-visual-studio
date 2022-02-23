using System;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.ElasticBeanstalk.Viewers;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;

using log4net;

namespace Amazon.AWSToolkit.Publish.Commands
{
    /// <summary>
    /// Creates a WPF command that opens a Beanstalk Environment for viewing
    /// </summary>
    public class BeanstalkEnvironmentViewerCommandFactory
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(BeanstalkEnvironmentViewerCommandFactory));

        public static ICommand Create(PublishToAwsDocumentViewModel viewModel)
        {
            return new RelayCommand(_ => CanView(viewModel), (obj) => ViewEnvironment(viewModel));
        }

        private static bool CanView(PublishToAwsDocumentViewModel viewModel)
        {
            return viewModel.PublishDestination?.DeploymentArtifact == DeploymentArtifact.BeanstalkEnvironment;
        }

        private static void ViewEnvironment(PublishToAwsDocumentViewModel viewModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(viewModel.PublishedArtifactId))
                {
                    throw new ArgumentException("No environment name");
                }

                var publishContext = viewModel.PublishContext;
                var viewer =
                    publishContext.ToolkitShellProvider.QueryAWSToolkitPluginService(typeof(IBeanstalkEnvironmentViewer)) as
                        IBeanstalkEnvironmentViewer;

                viewer.View(viewModel.PublishedArtifactId,
                    publishContext.ConnectionManager.ActiveCredentialIdentifier,
                    publishContext.ConnectionManager.ActiveRegion);
            }
            catch (Exception e)
            {
                viewModel.PublishContext.ToolkitShellProvider.OutputError(new Exception($"Error viewing Beanstalk environment {viewModel.PublishedArtifactId}", e), Logger);
            }
        }
    }
}
