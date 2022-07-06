using Amazon.S3.Model;

namespace Amazon.S3.IO
{
    public static class S3File
    {
        public static void Copy(IAmazonS3 s3Client,
            string sourceBucket, string sourcePath,
            string destinationBucket, string destinationPath)
        {
            if (sourceBucket == destinationBucket && sourcePath == destinationPath)
            {
                return;
            }

            var getACLRequest = new GetACLRequest() {BucketName = sourceBucket, Key = sourcePath};

            var getACLResponse = s3Client.GetACL(getACLRequest);

            // Workaround for issue reported in https://github.com/aws/aws-sdk-net/issues/1720
            // CopyObjectRequestMarshaller in AWS SDK for .NET trims the first leading / from both the source and destination keys
            // If the path has an empty folder at the root, it requires a leading /.  Workaround is to pad a leading / on both keys.
            s3Client.CopyObject(new CopyObjectRequest()
            {
                SourceBucket = sourceBucket,
                SourceKey = $"{S3Path.DefaultDirectorySeparator}{sourcePath}",
                DestinationBucket = destinationBucket,
                DestinationKey = $"{S3Path.DefaultDirectorySeparator}{destinationPath}",
                MetadataDirective = S3MetadataDirective.COPY
            });

            var getBucketOwnershipControlsResponse =
                s3Client.GetBucketOwnershipControls(
                    new GetBucketOwnershipControlsRequest() {BucketName = destinationBucket});

            // IDE-7806: Change in 11/2021 allows for ACLs to be disabled on a bucket and is the default configuration for newly created
            // buckets.  This will return an error from PutACL that will be displayed to the user.  GetACL continues to work as expected.
            // https://aws.amazon.com/about-aws/whats-new/2021/11/amazon-s3-object-ownership-simplify-access-management-data-s3/
            if (!getBucketOwnershipControlsResponse.OwnershipControls.Rules.Exists(rule =>
                    rule.ObjectOwnership == ObjectOwnership.BucketOwnerEnforced))
            {
                s3Client.PutACL(new PutACLRequest()
                {
                    BucketName = destinationBucket,
                    Key = destinationPath,
                    AccessControlList = getACLResponse.AccessControlList
                });
            }
        }

        public static void Move(IAmazonS3 s3Client,
            string sourceBucket, string sourcePath,
            string destinationBucket, string destinationPath)
        {
            if (sourceBucket == destinationBucket && sourcePath == destinationPath)
            {
                return;
            }

            Copy(s3Client, sourceBucket, sourcePath, destinationBucket, destinationPath);
            s3Client.DeleteObject(new DeleteObjectRequest()
            {
                BucketName = sourceBucket,
                Key = sourcePath
            });
        }
    }
}
