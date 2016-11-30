using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
