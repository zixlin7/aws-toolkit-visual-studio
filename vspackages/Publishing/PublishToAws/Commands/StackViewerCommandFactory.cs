﻿using System;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudFormation;
using Amazon.AWSToolkit.Commands;
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
            return new RelayCommand((obj) => ViewStack(viewModel));
        }

        private static void ViewStack(PublishToAwsDocumentViewModel viewModel)
        {
            try
            {
                var publishContext = viewModel.PublishContext;
                var cloudFormationViewer =
                    publishContext.ToolkitShellProvider.QueryAWSToolkitPluginService(typeof(ICloudFormationViewer)) as
                        ICloudFormationViewer;

                cloudFormationViewer.View(viewModel.PublishedStackName,
                    publishContext.ConnectionManager.ActiveCredentialIdentifier,
                    publishContext.ConnectionManager.ActiveRegion);
            }
            catch (Exception e)
            {
                viewModel.PublishContext.ToolkitShellProvider.OutputError(new Exception($"Error viewing CloudFormation stack {viewModel.PublishedStackName}", e), Logger);
            }
        }
    }
}
