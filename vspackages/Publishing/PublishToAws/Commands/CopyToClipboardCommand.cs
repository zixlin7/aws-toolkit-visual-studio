using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;

using log4net;

namespace Amazon.AWSToolkit.Publish.Commands
{
    /// <summary>
    /// Creates a WPF Command that copies details about published resources to clipboard
    /// </summary>
    public class CopyToClipboardCommand
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(CopyToClipboardCommand));

        public static ICommand Create(PublishToAwsDocumentViewModel viewModel)
        {
            return new RelayCommand((obj) => Copy(viewModel));
        }

        private static void Copy(PublishToAwsDocumentViewModel viewModel)
        {
            try
            {
                var content = CreateContent(viewModel.PublishResources);
                Clipboard.SetText(content);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to copy to clipboard details about published resources", e);
                viewModel.PublishContext.ToolkitShellProvider.OutputToHostConsole(
                    $"Error copying to clipboard details about published resources");
            }
        }

        public static string CreateContent(ObservableCollection<PublishResource> publishResources)
        {
            var sb = new StringBuilder();
            publishResources?.ToList().ForEach(resource =>
            {
                sb.Append(CreateResourceContent(resource));
            });
            return sb.ToString();
        }

        private static string CreateResourceContent(PublishResource resource)
        {
            var sb = new StringBuilder();

            sb.AppendLine(resource.Description);
            sb.AppendLine(resource.Type);
            sb.AppendLine(resource.Id);
            resource.Data?.ToList().ForEach(entry =>
            {
                sb.AppendLine($"{entry.Key}:");
                sb.AppendLine(entry.Value);
            });
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
