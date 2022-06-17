using System;
using System.Windows;
using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Shared;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Commands
{
    public class CopyArnCommand
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(CopyArnCommand));

        public static ICommand Create(BaseLogsViewModel viewModel, IAWSToolkitShellProvider shellProvider)
        {
            return new RelayCommand(obj => Execute(obj, viewModel, shellProvider));
        }

        private static void Execute(object parameter, BaseLogsViewModel viewModel, IAWSToolkitShellProvider shellProvider)
        {
            var result = Copy(parameter, viewModel.GetLogTypeDisplayName(), shellProvider);
            viewModel.RecordCopyArnMetric(result, GetArnResourceType(viewModel));
        }

        private static bool Copy(object parameter, string resourceType, IAWSToolkitShellProvider shellProvider)
        {
            try
            {
                var resourceArn = (string) parameter;
                Clipboard.SetText(resourceArn);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error($"Error copying arn for {resourceType}", e);
                shellProvider.OutputToHostConsole(
                    $"Error copying arn for {resourceType}");

                return false;
            }
        }

        private static CloudWatchResourceType GetArnResourceType(BaseLogsViewModel viewModel)
        {
            var sourceResourceType = viewModel.GetCloudWatchResourceType();

            if (CloudWatchResourceType.LogGroupList.Equals(sourceResourceType))
            {
                return CloudWatchResourceType.LogGroup;
            }

            if (CloudWatchResourceType.LogGroup.Equals(sourceResourceType))
            {
                return CloudWatchResourceType.LogStream;
            }

            return sourceResourceType;
        }
    }
}
