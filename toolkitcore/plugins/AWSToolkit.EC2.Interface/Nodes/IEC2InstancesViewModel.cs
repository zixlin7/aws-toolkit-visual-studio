using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public interface IEC2InstancesViewModel : IViewModel
    {
        void ConnectToInstance(string instanceId);
        void ConnectToInstance(IList<string> instanceIds);
    }
}
