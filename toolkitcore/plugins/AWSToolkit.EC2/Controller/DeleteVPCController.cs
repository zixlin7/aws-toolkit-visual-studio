using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.View;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class DeleteVPCController
    {
        ActionResults _results;
        string _vpcId;
        IAmazonEC2 _ec2Client;
        DeleteVPCControl _control;

        public ActionResults Execute(IAmazonEC2 ec2Client, string vpcId)
        {
            this._ec2Client = ec2Client;
            this._vpcId = vpcId;

            if (CheckIfInstancesUsingVPC())
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("This VPC cannot be deleted until all instances in the VPC have been terminated.");
                return new ActionResults().WithSuccess(false);
            }

            this._control = new DeleteVPCControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);

            return this._results;
        }

        private bool CheckIfInstancesUsingVPC()
        {
            var request = new DescribeInstancesRequest()
            {
                Filters = new List<Filter>()
                {
                    new Filter()
                    {
                        Name = "vpc-id",
                        Values = new List<string>(){this._vpcId}
                    }
                }
            };

            var result = this._ec2Client.DescribeInstances(request);
            if (result.Reservations.Count > 0)
                return true;

            return false;
        }

        public void DeleteVPC()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.DeleteVPCAsync));
        }

        void DeleteVPCAsync(object state)
        {
            try
            {
                disassociateRouteTables();
                deleteSecurityGroupIPPermissions();
                deleteNetworkInterfaces();
                deleteSecurityGroups();
                deleteInternetGateways();
                deleteSubnets();
                deleteNetworkACLs();
                deleteRouteTables();

                this._ec2Client.DeleteVpc(new DeleteVpcRequest() { VpcId = this._vpcId });

                this._results = new ActionResults() { Success = true };
                this._control.DeleteAsyncComplete(true);
            }
            catch (Exception e)
            {
                this._control.AppendOutputMessage(e.Message);
                this._control.DeleteAsyncComplete(false);
            }
        }

        void deleteSecurityGroupIPPermissions()
        {
            var vpcGroups = new HashSet<string>();
            var describResponse = this._ec2Client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest());

            foreach (var group in describResponse.SecurityGroups)
            {
                if (group.VpcId == this._vpcId)
                    vpcGroups.Add(group.GroupId);
            }

            foreach (var group in describResponse.SecurityGroups)
            {
                deleteSecurityGroupIPPermissions(group.GroupId, group.IpPermissions, false, vpcGroups);
                deleteSecurityGroupIPPermissions(group.GroupId, group.IpPermissionsEgress, true, vpcGroups);
            }
        }

        void deleteSecurityGroupIPPermissions(string groupId, List<IpPermission> permissions, bool egress, HashSet<string> vpcGroupIds)
        {
            foreach (var permission in permissions)
            {
                foreach (var pair in permission.UserIdGroupPairs)
                {
                    if (!vpcGroupIds.Contains(pair.GroupId))
                        continue;

                    IpPermission spec = new IpPermission()
                    {
                        UserIdGroupPairs = new List<UserIdGroupPair>() { pair },
                        Ipv4Ranges = permission.Ipv4Ranges,
                        IpProtocol = permission.IpProtocol
                    };

                    try
                    {
                        if (egress)
                        {
                            var revokeRequest = new RevokeSecurityGroupEgressRequest()
                            {
                                GroupId = groupId,
                                IpPermissions = new List<IpPermission>() { spec }
                            };
                            this._ec2Client.RevokeSecurityGroupEgress(revokeRequest);
                        }
                        else
                        {
                            var revokeRequest = new RevokeSecurityGroupIngressRequest()
                            {
                                GroupId = groupId,
                                IpPermissions = new List<IpPermission>() { spec }
                            };

                            this._ec2Client.RevokeSecurityGroupIngress(revokeRequest);
                        }

                        this._control.AppendOutputMessage("Deleted {2} rule from group {0} associated to group {1}", groupId, pair.GroupId, egress ? "egress" : "ingress");
                    }
                    catch (Exception e)
                    {
                        if (e is ApplicationException)
                            throw;

                        throw new ApplicationException(string.Format("Error deleting {2} rule from group {0} associated to group {1}", groupId, pair.GroupId, egress ? "egress" : "ingress"));
                    }
                }
            }
        }

        void deleteSecurityGroups()
        {
            try
            {
                var descResponse = this._ec2Client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest());
                foreach (var item in descResponse.SecurityGroups)
                {
                    if (!string.Equals(this._vpcId, item.VpcId, StringComparison.InvariantCultureIgnoreCase) || string.Equals("default", item.GroupName, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    try
                    {
                        this._ec2Client.DeleteSecurityGroup(new DeleteSecurityGroupRequest() { GroupId = item.GroupId });
                        this._control.AppendOutputMessage("Deleted security group {0}", item.GroupId);
                    }
                    catch (Exception e)
                    {
                        throw new ApplicationException(string.Format("Error deleting security group {0}: {1}", item.GroupId, e.Message));
                    }
                }
            }
            catch (Exception e)
            {
                if (e is ApplicationException)
                    throw;

                throw new ApplicationException(string.Format("Error deleting security groups for VPC: {0}", e.Message));
            }
        }

        void deleteSubnets()
        {
            try
            {
                var descResponse = this._ec2Client.DescribeSubnets(new DescribeSubnetsRequest());
                foreach (var item in descResponse.Subnets)
                {
                    if (!string.Equals(this._vpcId, item.VpcId, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    try
                    {
                        this._ec2Client.DeleteSubnet(new DeleteSubnetRequest() { SubnetId = item.SubnetId });
                        this._control.AppendOutputMessage("Deleted subnet {0}", item.SubnetId);
                    }
                    catch (Exception e)
                    {
                        if (e is ApplicationException)
                            throw;

                        throw new ApplicationException(string.Format("Error deleting subnet {0}: {1}", item.SubnetId, e.Message));
                    }
                }
            }
            catch (Exception e)
            {
                if (e is ApplicationException)
                    throw;

                throw new ApplicationException(string.Format("Error deleting subnets for VPC: {0}", e.Message));
            }
        }

        void deleteInternetGateways()
        {
            try
            {
                var descResponse = this._ec2Client.DescribeInternetGateways(new DescribeInternetGatewaysRequest());
                foreach (var item in descResponse.InternetGateways)
                {
                    foreach (var attachment in item.Attachments)
                    {
                        if (!string.Equals(this._vpcId, attachment.VpcId, StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        try
                        {
                            this._ec2Client.DetachInternetGateway(new DetachInternetGatewayRequest() { InternetGatewayId = item.InternetGatewayId, VpcId = this._vpcId });
                            this._control.AppendOutputMessage("Detaching internet security gateway {0}", item.InternetGatewayId);
                        }
                        catch (Exception e)
                        {
                            throw new ApplicationException(string.Format("Error detaching internet security gateway {0}: {1}", item.InternetGatewayId, e.Message));
                        }

                        try
                        {
                            // This was the only attachment do delete it.
                            if (item.Attachments.Count == 1)
                            {
                                this._ec2Client.DeleteInternetGateway(new DeleteInternetGatewayRequest(){InternetGatewayId = item.InternetGatewayId});
                                this._control.AppendOutputMessage("Deleted internet security gateway {0}", item.InternetGatewayId);
                            }
                        }
                        catch (Exception e)
                        {
                            throw new ApplicationException(string.Format("Error deleting internet security gateway {0}: {1}", item.InternetGatewayId, e.Message));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (e is ApplicationException)
                    throw;

                throw new ApplicationException(string.Format("Error deleting internet security gateways for VPC: {0}", e.Message));
            }
        }

        void deleteNetworkInterfaces()
        {
            try
            {
                var descResponse = this._ec2Client.DescribeNetworkInterfaces(new DescribeNetworkInterfacesRequest() { Filters = new List<Filter>() { new Filter() { Name = "vpc-id", Values = new List<string>() { this._vpcId } } } });

                foreach (var item in descResponse.NetworkInterfaces)
                {
                    if (item.Attachment != null && !string.IsNullOrEmpty(item.Attachment.AttachmentId))
                    {
                        try
                        {
                            this._ec2Client.DetachNetworkInterface(new DetachNetworkInterfaceRequest() { AttachmentId = item.Attachment.AttachmentId, Force = true });
                            this._control.AppendOutputMessage("Detached network interface {0} from instance {1}", item.NetworkInterfaceId, item.Attachment.InstanceId);
                        }
                        catch (Exception e)
                        {
                            throw new ApplicationException(string.Format("Error detaching network interface {0} from instance {1}: {2}", item.NetworkInterfaceId, item.Attachment.InstanceId, e.Message));
                        }
                    }

                    try
                    {
                        this._ec2Client.DeleteNetworkInterface(new DeleteNetworkInterfaceRequest() { NetworkInterfaceId = item.NetworkInterfaceId });
                        this._control.AppendOutputMessage("Deleted network interface {0}", item.NetworkInterfaceId);
                    }
                    catch (Exception e)
                    {
                        throw new ApplicationException(string.Format("Error deleting network interface {0}: {1}", item.NetworkInterfaceId, e.Message));
                    }
                }
            }
            catch (Exception e)
            {
                if (e is ApplicationException)
                    throw;

                throw new ApplicationException(string.Format("Error deleting network interfaces for VPC: {0}", e.Message));
            }
        }

        void disassociateRouteTables()
        {
            try
            {
                var descResponse = this._ec2Client.DescribeRouteTables(new DescribeRouteTablesRequest(){ Filters = new List<Filter>() { new Filter() { Name = "vpc-id", Values = new List<string>() { this._vpcId } } } });

                foreach(var item in descResponse.RouteTables)
                {
                    foreach(var association in item.Associations)
                    {
                        try
                        {
                            if (!association.Main)
                            {
                                this._ec2Client.DisassociateRouteTable(new DisassociateRouteTableRequest() { AssociationId = association.RouteTableAssociationId });
                                this._control.AppendOutputMessage("Disassociated route table {0} from subnet {1}", item.RouteTableId, association.SubnetId);
                            }
                        }
                        catch(Exception e)
                        {
                            throw new ApplicationException(string.Format("Error disassociating route table {0} from subnet {1}: {2}", item.RouteTableId, association.SubnetId, e.Message));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (e is ApplicationException)
                    throw;
                throw new ApplicationException(string.Format("Error disassociating route tables for VPC: {0}", e.Message));
            }
        }

        void deleteRouteTables()
        {
            try
            {
                var descResponse = this._ec2Client.DescribeRouteTables(new DescribeRouteTablesRequest() { Filters = new List<Filter>() { new Filter() { Name = "vpc-id", Values = new List<string>() { this._vpcId } } } });

                foreach (var item in descResponse.RouteTables)
                {
                    try
                    {
                        if (item.Associations.Count == 0 || item.Associations.FirstOrDefault(x => !x.Main) != null)
                        {
                            this._ec2Client.DeleteRouteTable(new DeleteRouteTableRequest() { RouteTableId = item.RouteTableId });
                            this._control.AppendOutputMessage("Deleted route table {0}", item.RouteTableId);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new ApplicationException(string.Format("Error deleting route table {0} : {1}", item.RouteTableId, e.Message));
                    }
                }
            }
            catch (Exception e)
            {
                if (e is ApplicationException)
                    throw;
                throw new ApplicationException(string.Format("Error deleting route tables for VPC: {0}", e.Message));
            }
        }

        void deleteNetworkACLs()
        {
            try
            {
                var descResponse = this._ec2Client.DescribeNetworkAcls(new DescribeNetworkAclsRequest(){ Filters = new List<Filter>() { new Filter() { Name = "vpc-id", Values = new List<string>() { this._vpcId } } } });

                foreach (var item in descResponse.NetworkAcls)
                {
                    try
                    {
                        if (!item.IsDefault)
                        {
                            this._ec2Client.DeleteNetworkAcl(new DeleteNetworkAclRequest() { NetworkAclId = item.NetworkAclId });
                            this._control.AppendOutputMessage("Deleted network acl {0}", item.NetworkAclId);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new ApplicationException(string.Format("Error deleting network acl {0} : {1}", item.NetworkAclId, e.Message));
                    }
                }
            }
            catch (Exception e)
            {
                if (e is ApplicationException)
                    throw;
                throw new ApplicationException(string.Format("Error deleting network acls for VPC: {0}", e.Message));
            }
        }
    }
}
