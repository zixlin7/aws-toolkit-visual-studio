using System.Collections.Generic;
using Amazon.S3;

namespace Amazon.AWSToolkit.S3
{
    public static class S3Constants
    {
        public const int MULTIPLE_OBJECT_DELETE_LIMIT = 1000;

        public static readonly HashSet<S3StorageClass> LIST_OF_KEYS_GLACIER_STORAGE_CLASS;
        public static readonly HashSet<S3StorageClass> LIST_OF_KEYS_NONGLACIER_STORAGE_CLASS;

        public const string NOTIFICATION_FILTER_KEY_PREFIX = "prefix";
        public const string NOTIFICATION_FILTER_KEY_SUFFIX = "suffix";

        static S3Constants()
        {
            LIST_OF_KEYS_GLACIER_STORAGE_CLASS = new HashSet<S3StorageClass>() { S3StorageClass.Glacier };
            LIST_OF_KEYS_NONGLACIER_STORAGE_CLASS = new HashSet<S3StorageClass>() { S3StorageClass.Standard, S3StorageClass.ReducedRedundancy };
        }

    }
}
