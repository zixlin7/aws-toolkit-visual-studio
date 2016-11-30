using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

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

            s3Client.CopyObject(new CopyObjectRequest()
            {
                SourceBucket = sourceBucket,
                SourceKey = sourcePath,
                DestinationBucket = destinationBucket,
                DestinationKey = destinationPath,
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

            Copy(s3Client, sourceBucket, sourcePath, destinationBucket, destinationPath);
            s3Client.DeleteObject(new DeleteObjectRequest()
            {
                BucketName = sourceBucket,
                Key = sourcePath
            });
        }

        public static string GetName(string fullpath)
        {
            int pos = fullpath.LastIndexOf('/');
            if (pos < 0)
                return fullpath;
            return fullpath.Substring(pos + 1);
        }
    }
}
