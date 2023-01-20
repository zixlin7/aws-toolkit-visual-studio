using System.Collections.Generic;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public interface IIPPermissionController
    {
        void RefreshPermission(EC2Constants.PermissionType PermissionType);
        ActionResults AddPermission(EC2Constants.PermissionType PermissionType);
        ActionResults DeletePermission(IList<IPPermissionWrapper> toBeDeleted, EC2Constants.PermissionType PermissionType);

        void RecordEditSecurityGroupPermission(ActionResults result);
    }
}
