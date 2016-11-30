using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.EC2.Model
{
    public interface ISubnetAssociationWrapper : IWrapper
    {
        string VpcId { get; }

        bool CanDisassociate { get; }
    }
}
