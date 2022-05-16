using System;
using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Shared;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Commands
{
    public class CopyArnCommand
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(CopyArnCommand));

        public static ICommand Create(string resourceType, IAWSToolkitShellProvider shellProvider)
        {
            return new RelayCommand(obj => Copy(obj, resourceType, shellProvider));
        }

        private static void Copy(object parameter, string resourceType, IAWSToolkitShellProvider shellProvider)
        {
            try
            {
                var resourceArn = (string) parameter;
                Clipboard.SetText(resourceArn);
            }
            catch (Exception e)
            {
                Logger.Error($"Error copying arn for {resourceType}", e);
                shellProvider.OutputToHostConsole(
                    $"Error copying arn for {resourceType}");
            }
        }
    }
}
