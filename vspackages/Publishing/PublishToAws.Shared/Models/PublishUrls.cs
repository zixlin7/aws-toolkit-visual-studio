using System;

namespace Amazon.AWSToolkit.Publish.Models
{
    public class PublishUrls
    {
        public const string PublishToAwsIssuesUrl = "https://github.com/aws/aws-toolkit-visual-studio/labels/publish";
        public static readonly Uri PublishToAwsIssuesUri = new Uri(PublishToAwsIssuesUrl);
    }
}
