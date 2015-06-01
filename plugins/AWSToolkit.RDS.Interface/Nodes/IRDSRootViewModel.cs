using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.RDS;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public interface IRDSRootViewModel
    {
        IAmazonRDS RDSClient { get; }
    }
}
