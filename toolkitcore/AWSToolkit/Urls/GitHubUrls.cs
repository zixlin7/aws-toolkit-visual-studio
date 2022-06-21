using System;

namespace Amazon.AWSToolkit.Urls
{
    public class GitHubUrls
    {
        public const string ReleaseNotesUrl = "https://github.com/aws/aws-toolkit-visual-studio/blob/main/CHANGELOG.md";
        public static readonly Uri ReleaseNotesUri = new Uri(ReleaseNotesUrl);

        public const string CreateNewIssueUrl = "https://github.com/aws/aws-toolkit-visual-studio/issues/new/choose";
        public static readonly Uri CreateNewIssueUri = new Uri(CreateNewIssueUrl);
    }
}
