using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.SQS;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SQS.Nodes
{
    public interface ISQSRootViewModel : IServiceRootViewModel
    {
        IAmazonSQS SQSClient { get; }
    }
}
