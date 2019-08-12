using Amazon.SimpleNotificationService.Model;

namespace Amazon.SimpleNotificationService
{
    public static class AmazonSimpleNotificationServiceClientExt
    {
        public static SetTopicAttributesResponse SetDisplayName(this IAmazonSimpleNotificationService client, string topicARN, string displayName)
        {
            SetTopicAttributesRequest request = new SetTopicAttributesRequest()
            {
                 TopicArn = topicARN,
                 AttributeName = GetTopicAttributesResponseExt.DISPLAY_NAME,
                 AttributeValue = displayName
            };

            return client.SetTopicAttributes(request);
        }

        public static SetTopicAttributesResponse SetTopicAttribute(this IAmazonSimpleNotificationService client, string topicARN, string name, string value)
        {
            SetTopicAttributesRequest request = new SetTopicAttributesRequest()
            {
                 TopicArn = topicARN,
                 AttributeName = name,
                 AttributeValue = value
            };

            return client.SetTopicAttributes(request);
        }

        public static SetTopicAttributesResponse SetTopicAttribute(this AmazonSimpleNotificationServiceClient client, string topicARN, string name, string value)
        {
            SetTopicAttributesRequest request = new SetTopicAttributesRequest()
            {
                TopicArn = topicARN,
                AttributeName = name,
                AttributeValue = value
            };

            return client.SetTopicAttributes(request);
        }
    }
}
