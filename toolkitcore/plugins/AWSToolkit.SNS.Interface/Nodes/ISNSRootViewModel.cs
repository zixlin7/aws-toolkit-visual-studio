using Amazon.SimpleNotificationService;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SNS.Nodes
{
    public interface ISNSRootViewModel : IServiceRootViewModel
    {
        IAmazonSimpleNotificationService SNSClient { get; }

        void AddTopic(string topicArn);
        void RemoveTopic(string name);
    }
}
