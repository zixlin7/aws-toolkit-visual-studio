using System.Collections.Generic;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public interface IAssociateSubnetController
    {
        AssociateSubnetModel Model { get; }
        IList<SubnetWrapper> GetAvailableSubnets();
        void AssociateSubnet();
    }
}
