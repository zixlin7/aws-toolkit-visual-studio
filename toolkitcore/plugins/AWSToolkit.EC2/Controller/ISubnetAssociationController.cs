using System.Collections.Generic;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public interface ISubnetAssociationController
    {
        object AddSubnetAssociation(IWrapper associatedItem);

        void DisassociateSubnets(string vpcId, IEnumerable<string> associationIds);

        void RefreshAssociations(IWrapper associatedItem);
    }
}
