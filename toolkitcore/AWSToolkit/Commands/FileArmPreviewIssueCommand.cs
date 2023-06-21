using System;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Amazon.AWSToolkit.Shared;

using log4net;

namespace Amazon.AWSToolkit.Commands
{
    /// <summary>
    /// Takes users to a pre-filled out bug report template for the ARM Preview.
    /// </summary>
    public class FileArmPreviewIssueCommand : AsyncCommand
    {
        public const string Title = "File issue";
        private const string _baseNewIssueUrl = "https://github.com/aws/aws-toolkit-visual-studio/issues/new?labels=bug";
        private const string _issueTitle = "Arm64 Preview: Bug Report";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(FileArmPreviewIssueCommand));
        private readonly IAWSToolkitShellProvider _shellProvider;

        public FileArmPreviewIssueCommand(IAWSToolkitShellProvider shellProvider)
        {
            _shellProvider = shellProvider;
        }

        protected override Task ExecuteCoreAsync(object _)
        {
            try
            {
                var issueUrl =
                    _baseNewIssueUrl
                    + $"&title={HttpUtility.UrlEncode(_issueTitle)}"
                    + $"&body={HttpUtility.UrlEncode(CreateBodyText(_shellProvider))}";

                _shellProvider.OpenInBrowser(issueUrl, false);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to open url", ex);
                _shellProvider.OutputToHostConsole($"Error opening url in browser: {ex.Message}", true);
            }

            return Task.CompletedTask;
        }

        private static string CreateBodyText(IAWSToolkitShellProvider shellProvider)
        {
            var body = new StringBuilder();
            body.AppendLine(
                "I found a problem while using the Preview version of the AWS Toolkit for Arm64 Visual Studio.");
            body.AppendLine();
            body.AppendLine(
                "<!-- Explain the bug you found. Please include specific details that can help reproduce the problem. -->");
            body.AppendLine();
            body.AppendLine($"Toolkit: {shellProvider.ProductEnvironment.AwsProductVersion}");
            body.AppendLine($"Visual Studio: {shellProvider.ProductEnvironment.ParentProductVersion}");

            return body.ToString();
        }
    }
}
