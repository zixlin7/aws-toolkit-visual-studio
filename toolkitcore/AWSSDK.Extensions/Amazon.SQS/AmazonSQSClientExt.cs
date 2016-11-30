using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.SQS.Model;

namespace Amazon.SQS
{
    public static class AmazonSQSClientExt
    {
        public static SetQueueAttributesResponse SetQueueAttribute(this IAmazonSQS client, string queueUrl, string name, string value)
        {
            SetQueueAttributesRequest request = new SetQueueAttributesRequest()
            {
                Attributes = new Dictionary<string, string>() { { name, value } },
                QueueUrl = queueUrl
            };

            return client.SetQueueAttributes(request);
        }

        public static SetQueueAttributesResponse SetQueueAttribute(this AmazonSQSClient client, string queueUrl, string name, string value)
        {
            SetQueueAttributesRequest request = new SetQueueAttributesRequest()
            {
                Attributes = new Dictionary<string, string>() { { name, value } },
                QueueUrl = queueUrl
            };

            return client.SetQueueAttributes(request);
        }

        public static string GetQueueARN(this IAmazonSQS client, string region, string queueUrl)
        {
            return getQueueARN(client, region, queueUrl);
        }

        public static string GetQueueARN(this AmazonSQSClient client, string region, string queueUrl)
        {
            return getQueueARN(client, region, queueUrl);
        }


        private static string getQueueARN(IAmazonSQS client, string region, string queueUrl)
        {
            int pos = queueUrl.LastIndexOf('/');
            string name = queueUrl.Substring(pos + 1);
            int secondToLastPos = queueUrl.LastIndexOf('/', pos - 1);
            string id = queueUrl.Substring(secondToLastPos + 1, pos - (secondToLastPos + 1));

            string arn = string.Format("arn:aws:sqs:{0}:{1}:{2}", region, id, name);
            return arn;
        }
    }
}
