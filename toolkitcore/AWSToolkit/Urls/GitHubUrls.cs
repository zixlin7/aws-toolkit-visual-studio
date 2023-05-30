using System;

namespace Amazon.AWSToolkit.Urls
{
    public class GitHubUrls
    {
        public const string ReleaseNotesUrl = "https://github.com/aws/aws-toolkit-visual-studio/blob/main/CHANGELOG.md";
        public static readonly Uri ReleaseNotesUri = new Uri(ReleaseNotesUrl);

        public const string CreateNewIssueUrl = "https://github.com/aws/aws-toolkit-visual-studio/issues/new/choose";
        public static readonly Uri CreateNewIssueUri = new Uri(CreateNewIssueUrl);

        public const string BugReportUrl = "https://github.com/aws/aws-toolkit-visual-studio/issues/new?assignees=&labels=bug&template=bug_report.md";
        public static readonly Uri BugReportUri = new Uri(BugReportUrl);

        public const string FeatureRequestUrl = " https://github.com/aws/aws-toolkit-visual-studio/issues/new?assignees=&labels=feature-request&template=feature_request.md";
        public static readonly Uri FeatureRequestUri = new Uri(FeatureRequestUrl);

    }
}
