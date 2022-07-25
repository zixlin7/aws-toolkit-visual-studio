using System;

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

            try
            {
                s3Client.PutACL(new PutACLRequest()
                {
                    BucketName = destinationBucket,
                    Key = destinationPath,
                    AccessControlList = getACLResponse.AccessControlList
                });
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.ErrorCode != "AccessControlListNotSupported")
                {
                    throw;
                }
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
