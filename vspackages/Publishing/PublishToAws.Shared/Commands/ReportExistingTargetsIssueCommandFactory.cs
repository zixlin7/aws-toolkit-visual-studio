using System;
using System.Text;
using System.Web;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Shared;

using log4net;

namespace Amazon.AWSToolkit.Publish.Commands
{
    public static class ReportExistingTargetsIssueCommandFactory
    {
        private const string BaseNewIssueUrl = "https://github.com/aws/aws-toolkit-visual-studio/issues/new?labels=publish";
        private const string IssueTitle = "Publish to AWS: I could not deploy to an existing cloud app";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ReportExistingTargetsIssueCommandFactory));

        public static ICommand Create(IAWSToolkitShellProvider shellProvider) => new RelayCommand(_ => Execute(shellProvider));

        private static void Execute(IAWSToolkitShellProvider shellProvider)
        {
            try
            {
                string issueUrl =
                    BaseNewIssueUrl
                    + $"&title={HttpUtility.UrlEncode(IssueTitle)}"
                    + $"&body={HttpUtility.UrlEncode(CreateBodyText(shellProvider))}";

                shellProvider.OpenInBrowser(issueUrl, false);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open url", ex);
                shellProvider.OutputToHostConsole($"Error opening url in browser: {ex.Message}", true);
            }
        }

        private static string CreateBodyText(IAWSToolkitShellProvider shellProvider)
        {
            var body = new StringBuilder();
            body.AppendLine(
                "In Publish to AWS, the Existing Targets list did not show my project. I expected to see it there.");
            body.AppendLine();
            body.AppendLine(
                "<!-- Explain why your project should have been listed. Please include any project details and how you previously deployed it. -->");
            body.AppendLine();
            body.AppendLine($"Toolkit: {shellProvider.ProductEnvironment.AwsProductVersion}");
            body.AppendLine($"Visual Studio: {shellProvider.ProductEnvironment.ParentProductVersion}");

            return body.ToString();
        }
    }
}
