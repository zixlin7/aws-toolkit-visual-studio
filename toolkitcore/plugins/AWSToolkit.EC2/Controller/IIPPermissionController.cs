using System.Collections.Generic;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public interface IIPPermissionController
    {
        void RefreshPermission(EC2Constants.PermissionType PermissionType);
        void AddPermission(EC2Constants.PermissionType PermissionType);
        void DeletePermission(IList<IPPermissionWrapper> toBeDeleted, EC2Constants.PermissionType PermissionType);
    }
}
