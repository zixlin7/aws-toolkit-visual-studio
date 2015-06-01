using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using Amazon.S3.Model;

namespace Amazon.S3
{
    public static class AmazonS3ClientExt
    {
        public static Uri GetPublicURL(this IAmazonS3 client, string bucketName, string key)
        {
            return getPublicURL(client, bucketName, key);
        }

        public static Uri GetPublicURL(this AmazonS3Client client, string bucketName, string key)
        {
            return getPublicURL(client, bucketName, key);
        }

        private static Uri getPublicURL(IAmazonS3 client, string bucketName, string key)
        {
            var request = new GetPreSignedUrlRequest()
            {
                BucketName = bucketName,
                Key = key,
                Expires = DateTime.Now.AddDays(1)
            };
            string url = client.GetPreSignedURL(request);
            int pos = url.IndexOf('?');
            if (pos > 0)
            {
                url = url.Substring(0, pos);
            }

            return new Uri(url);
        }


        public static void SetReducedRedundancyNotification(this IAmazonS3 client, string bucketName, string snsTopic)
        {
            setReducedRedundancyNotification(client, bucketName, snsTopic);
        }

        public static void SetReducedRedundancyNotification(this AmazonS3Client client, string bucketName, string snsTopic)
        {
            setReducedRedundancyNotification(client, bucketName, snsTopic);
        }

        private static void setReducedRedundancyNotification(IAmazonS3 client, string bucketName, string snsTopic)
        {
            var request = new PutBucketNotificationRequest()
            {
                BucketName = bucketName
            };

            var list = new TopicConfiguration();
            if (!string.IsNullOrEmpty(snsTopic))
            {
                var topic = new TopicConfiguration()
                {
                    Event = NotificationEvents.ReducedRedundancyLostObject,
                    Topic = snsTopic
                };
                request.TopicConfigurations.Add(topic);
            }

            client.PutBucketNotification(request);
        }
    }
}
