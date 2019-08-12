namespace Amazon.SimpleNotificationService.Model
{
    public static class GetTopicAttributesResponseExt
    {
        public const string TOPIC_ARN = "TopicArn";
        public const string OWNER = "Owner";
        public const string POLICY = "Policy";
        public const string DISPLAY_NAME = "DisplayName";


        public static string GetTopicARN(this GetTopicAttributesResponse result)
        {
            return getAttributeValue(result, TOPIC_ARN);
        }

        public static string GetOwner(this GetTopicAttributesResponse result)
        {
            return getAttributeValue(result, OWNER);
        }

        public static string GetPolicy(this GetTopicAttributesResponse result)
        {
            return getAttributeValue(result, POLICY);
        }

        public static string GetDisplayName(this GetTopicAttributesResponse result)
        {
            return getAttributeValue(result, DISPLAY_NAME);
        }


        private static string getAttributeValue(GetTopicAttributesResponse result, string field)
        {
            string value = null;
            result.Attributes.TryGetValue(field, out value);
            return value;
        }
    }
}
