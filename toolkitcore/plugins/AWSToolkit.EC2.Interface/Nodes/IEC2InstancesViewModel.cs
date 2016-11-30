using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator.Node;

using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public interface IEC2InstancesViewModel : IViewModel
    {
        void ConnectToInstance(string instanceId);
        void ConnectToInstance(IList<string> instanceIds);
    }
}
