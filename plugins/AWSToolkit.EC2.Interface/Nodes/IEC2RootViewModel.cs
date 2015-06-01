using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator.Node;

using Amazon.EC2;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public interface IEC2RootViewModel : IServiceRootViewModel
    {
        IAmazonEC2 EC2Client { get; }
    }
}
