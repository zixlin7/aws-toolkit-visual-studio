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
                return;

            var getACLRequest = new GetACLRequest()
            {
                BucketName = sourceBucket,
                Key = sourcePath
            };

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

            s3Client.PutACL(new PutACLRequest()
            {
                BucketName = destinationBucket,
                Key = destinationPath,
                AccessControlList = getACLResponse.AccessControlList
            });
        }

        public static void Move(IAmazonS3 s3Client,
            string sourceBucket, string sourcePath,
            string destinationBucket, string destinationPath)
        {
            if (sourceBucket == destinationBucket && sourcePath == destinationPath)
                return;

            // TODO If bucket doesn't have ACL (see line 31 above), copy will fail and DeleteObject
            // will not be executed.  Bucket ACL handling needs to be improved.  See IDE-7806
            Copy(s3Client, sourceBucket, sourcePath, destinationBucket, destinationPath);
            s3Client.DeleteObject(new DeleteObjectRequest()
            {
                BucketName = sourceBucket,
                Key = sourcePath
            });
        }
    }
}
