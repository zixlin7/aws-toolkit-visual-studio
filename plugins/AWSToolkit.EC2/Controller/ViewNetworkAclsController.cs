using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewNetworkAclsController : FeatureController<ViewNetworkAclsModel>, ISubnetAssociationController
    {
        ViewNetworkAclsControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewNetworkAclsControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            RefreshNetworkAcls();
        }

        public void RefreshNetworkAcls()
        {
            var subnets = this.EC2Client.DescribeSubnets(new DescribeSubnetsRequest()).Subnets;
            var response = this.EC2Client.DescribeNetworkAcls(new DescribeNetworkAclsRequest());
            
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                this.Model.NetworkAcls.Clear();
                foreach (var item in response.NetworkAcls)
                {
                    this.Model.NetworkAcls.Add(new NetworkAclWrapper(item, subnets));
                }
            }));
        }

        public NetworkAclWrapper CreateNetworkAcl()
        {
            var controller = new CreateNetworkAclController();
            var result = controller.Execute(this.EC2Client);
            if (result.Success)
            {
                RefreshNetworkAcls();
                foreach (var item in Model.NetworkAcls)
                {
                    if (result.FocalName.Equals(item.NetworkAclId))
                        return item;
                }
            }
            return null;
        }

        public void DeleteNetworkAcl(NetworkAclWrapper networkAcl)
        {
            var request = new DeleteNetworkAclRequest() { NetworkAclId = networkAcl.NetworkAclId };
            this.EC2Client.DeleteNetworkAcl(request);
        }

        public object AddSubnetAssociation(IWrapper wrapper)
        {
            var networkAcl = wrapper as NetworkAclWrapper;

            var controller = new AssociateSubnetToNetworkAclController();
            var results = controller.Execute(this.EC2Client, networkAcl);
            if (results.Success)
            {
                this.RefreshAssociations(networkAcl);
                foreach (var association in networkAcl.Associations)
                {
                    if (string.Equals(association.SubnetId, results.FocalName))
                        return association;
                }
            }

            return null;
        }

        public void DisassociateSubnets(string vpcId, IEnumerable<string> associationIds)
        {
            var mainNetworkAcl = this.Model.NetworkAcls.FirstOrDefault(x => x.VpcId == vpcId && x.NativeNetworkAcl.IsDefault);
            if (mainNetworkAcl == null)
                return;

            foreach (var associationid in associationIds)
            {
                var request = new ReplaceNetworkAclAssociationRequest() { AssociationId = associationid, NetworkAclId = mainNetworkAcl.NetworkAclId };
                this.EC2Client.ReplaceNetworkAclAssociation(request);
            }
        }

        public void RefreshAssociations(IWrapper wrapper)
        {
            var subnets = this.EC2Client.DescribeSubnets(new DescribeSubnetsRequest()).Subnets;
            var request = new DescribeNetworkAclsRequest();
            var response = this.EC2Client.DescribeNetworkAcls(request);
            var refreshedNetworkacls = response.NetworkAcls;

            
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                foreach (var networkAcl in this.Model.NetworkAcls)
                {
                    networkAcl.Associations.Clear();
                    var refreshedNetworkacl = refreshedNetworkacls.FirstOrDefault(x => x.NetworkAclId == networkAcl.NetworkAclId);

                    if (refreshedNetworkacl != null)
                    {
                        foreach (var item in refreshedNetworkacl.Associations)
                        {
                            if (string.IsNullOrEmpty(item.SubnetId))
                                continue;

                            var subnet = subnets.FirstOrDefault(x => x.SubnetId == item.SubnetId);
                            networkAcl.Associations.Add(new NetworkAclAssociationWrapper(item, subnet));
                        }
                    }
                }
            }));
        }

        public void AddRule(NetworkAclWrapper networkAcl, EC2Constants.PermissionType permissionType)
        {
            var controller = new AddNetworkAclRuleController();
            var results = controller.Execute(this.EC2Client, networkAcl.NetworkAclId, permissionType);
            if (results.Success)
            {
                this.RefreshRules(networkAcl, permissionType);
            }
        }

        public void DeleteRules(NetworkAclWrapper networkAcl, IEnumerable<NetworkAclEntryWrapper> toBeDeleted, EC2Constants.PermissionType permissionType)
        {
            foreach (var entry in toBeDeleted)
            {
                if (entry.NativeNetworkAclEntry.RuleNumber == NetworkAclEntryWrapper.DEFAULT_RULE_NUMBER)
                    continue;

                var request = new DeleteNetworkAclEntryRequest()
                {
                    Egress = permissionType == EC2Constants.PermissionType.Egrees,
                    RuleNumber = entry.NativeNetworkAclEntry.RuleNumber,
                    NetworkAclId = networkAcl.NetworkAclId
                };
                this.EC2Client.DeleteNetworkAclEntry(request);
            }
        }

        public void RefreshRules(NetworkAclWrapper networkAcl, EC2Constants.PermissionType permissionType)
        {
            var request = new DescribeNetworkAclsRequest()
            {
                NetworkAclIds = new List<string>(){networkAcl.NetworkAclId}
            };
            var response = this.EC2Client.DescribeNetworkAcls(request);

            if(response.NetworkAcls.Count != 1)
                return;

            var refreshedNetworkAcl = response.NetworkAcls[0];

            IList<NetworkAclEntryWrapper> entries = permissionType == EC2Constants.PermissionType.Egrees ? networkAcl.EgressEntries : networkAcl.IngressEntries;

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                entries.Clear();
                foreach (var item in refreshedNetworkAcl.Entries)
                {
                    if (permissionType == EC2Constants.PermissionType.Egrees && !item.Egress)
                        continue;
                    if (permissionType == EC2Constants.PermissionType.Ingress && item.Egress)
                        continue;

                    entries.Add(new NetworkAclEntryWrapper(item));
                }
            }));
        }
    }
}
