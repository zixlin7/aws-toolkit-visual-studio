using System;

namespace Amazon.AWSToolkit.Urls
{
    public class GitHubUrls
    {
        public const string ReleaseNotesUrl = "https://github.com/aws/aws-toolkit-visual-studio/blob/master/CHANGELOG.md";
        public static readonly Uri ReleaseNotesUri = new Uri(ReleaseNotesUrl);
    }
}
