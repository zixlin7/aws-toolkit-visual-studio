using Amazon.SQS;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SQS.Nodes
{
    public interface ISQSRootViewModel : IServiceRootViewModel
    {
        IAmazonSQS SQSClient { get; }
    }
}
