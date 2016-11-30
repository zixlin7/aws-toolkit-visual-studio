using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.SimpleNotificationService;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SNS.Nodes
{
    public interface ISNSTopicViewModel : IViewModel
    {
        IAmazonSimpleNotificationService SNSClient { get; }

        string TopicArn { get; }
    }
}
