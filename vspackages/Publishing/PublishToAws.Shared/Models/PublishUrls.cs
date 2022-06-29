using System;

namespace Amazon.AWSToolkit.Publish.Models
{
    public class PublishUrls
    {
        public const string PublishToAwsIssuesUrl = "https://github.com/aws/aws-toolkit-visual-studio/labels/publish";
        public static readonly Uri PublishToAwsIssuesUri = new Uri(PublishToAwsIssuesUrl);

        // TODO : Update this URL to https://aws.github.io/aws-dotnet-deploy/troubleshooting-guide/ when it no longer shows a 404 error
        public const string TroubleshootingGuideUrl = "https://aws.github.io/aws-dotnet-deploy/troubleshooting-guide/other-issues/";
        public static readonly Uri TroubleshootingGuideUri = new Uri(TroubleshootingGuideUrl);
    }
}
