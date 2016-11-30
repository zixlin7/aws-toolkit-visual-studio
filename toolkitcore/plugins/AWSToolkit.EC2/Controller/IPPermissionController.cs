using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class IPPermissionController : IIPPermissionController
    {
        FeatureViewModel _viewModel;
        IAmazonEC2 _ec2Client;
        SecurityGroupWrapper _owningGroup;

        public IPPermissionController(FeatureViewModel viewModel, SecurityGroupWrapper owningGroup)
        {
            this._viewModel = viewModel;
            this._ec2Client = viewModel.EC2Client;
            this._owningGroup = owningGroup;
        }

        public void RefreshPermission(EC2Constants.PermissionType permisionType)
        {
            if (this._owningGroup == null)
                return;

            var request = new DescribeSecurityGroupsRequest(){GroupIds = new List<string>(){this._owningGroup.NativeSecurityGroup.GroupId}};
            var response = this._ec2Client.DescribeSecurityGroups(request);

            if (response.SecurityGroups.Count == 1)
            {
                var permissions = permisionType == EC2Constants.PermissionType.Ingress ?
                    response.SecurityGroups[0].IpPermissions :
                    response.SecurityGroups[0].IpPermissionsEgress;

                this._owningGroup.ReloadIpPermissions(permissions, permisionType);
            }
        }

        public void AddPermission(EC2Constants.PermissionType permisionType)
        {
            AddIPPermissionController controller = new AddIPPermissionController();
            var results = controller.Execute(this._viewModel, this._owningGroup.NativeSecurityGroup, permisionType);
            if (results.Success)
            {
                this.RefreshPermission(permisionType);
            }
        }

        public void DeletePermission(IList<IPPermissionWrapper> toBeDeleted, EC2Constants.PermissionType permisionType)
        {
            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete IP Permission", 
                                                               "Are you sure you want to delete the ip permission(s)"))
                return;

            foreach (var ipPermission in toBeDeleted)
            {
                IpPermission permission = new IpPermission();

                permission.IpProtocol = ipPermission.IPProtocol.ToLower();
                if (ipPermission.FromPort != 0)
                    permission.FromPort = ipPermission.FromPort;
                if (ipPermission.ToPort != 0)
                    permission.ToPort = ipPermission.ToPort;

                if (!string.IsNullOrEmpty(ipPermission.UserId))
                {
                    UserIdGroupPair pair = new UserIdGroupPair();
                    pair.UserId = ipPermission.UserId;

                    if (!string.IsNullOrEmpty(this._owningGroup.VpcId))
                        pair.GroupId = ipPermission.GroupName;
                    else
                        pair.GroupName = ipPermission.GroupName;

                    permission.UserIdGroupPairs.Add(pair);
                }
                else
                {
                    if (!string.IsNullOrEmpty(ipPermission.Source))
                        permission.IpRanges = new List<string>(){ipPermission.Source};
                }

                if (permisionType == EC2Constants.PermissionType.Ingress)
                {
                    RevokeSecurityGroupIngressRequest request = new RevokeSecurityGroupIngressRequest();
                    request.GroupId = this._owningGroup.GroupId;
                    request.IpPermissions.Add(permission);
                    this._ec2Client.RevokeSecurityGroupIngress(request);
                }
                else
                {
                    RevokeSecurityGroupEgressRequest request = new RevokeSecurityGroupEgressRequest();
                    request.GroupId = this._owningGroup.GroupId;
                    request.IpPermissions.Add(permission);
                    this._ec2Client.RevokeSecurityGroupEgress(request);
                }
            }

            this.RefreshPermission(permisionType);
        }
    }
}
