using System.Collections.Generic;
using Amazon.S3;

namespace Amazon.AWSToolkit.S3
{
    public static class S3Constants
    {
        public const int MULTIPLE_OBJECT_DELETE_LIMIT = 1000;

        public const string NOTIFICATION_FILTER_KEY_PREFIX = "prefix";
        public const string NOTIFICATION_FILTER_KEY_SUFFIX = "suffix";
    }
}
