using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Util;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class AssociateSubnetToNetworkAclController : IAssociateSubnetController
    {
        ActionResults _results;
        AssociateSubnetModel _model;
        IAmazonEC2 _ec2Client;
        NetworkAclWrapper _networkAcl;

        public ActionResults Execute(IAmazonEC2 ec2Client, NetworkAclWrapper networkAcl)
        {
            this._ec2Client = ec2Client;
            this._networkAcl = networkAcl;
            this._model = new AssociateSubnetModel();
            this._model.AvailableSubnets = this.GetAvailableSubnets();

            if (this.Model.AvailableSubnets.Count == 0)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("There are no subnets to associated with this network acl.");
                return new ActionResults().WithSuccess(false);
            }
            this._model.SelectedSubnet = this._model.AvailableSubnets[0];

            var control = new AssociateSubnetControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);

            return this._results;
        }

        public AssociateSubnetModel Model
        {
            get { return this._model; }
        }

        public IList<SubnetWrapper> GetAvailableSubnets()
        {
            var subnets = new List<SubnetWrapper>();

            var ec2Subnets = this._ec2Client.DescribeSubnets(new DescribeSubnetsRequest()).Subnets;

            foreach (var subnet in ec2Subnets)
            {
                if (!string.Equals(subnet.VpcId, this._networkAcl.VpcId) || this._networkAcl.Associations.FirstOrDefault(x => x.SubnetId == subnet.SubnetId) != null)
                    continue;

                subnets.Add(new SubnetWrapper(subnet, null, null));
            }

            return subnets;
        }

        public void AssociateSubnet()
        {
            var associationId = FindCurrentAssociation();
            if (associationId == null)
                return;

            var request = new ReplaceNetworkAclAssociationRequest() { NetworkAclId = this._networkAcl.NetworkAclId, AssociationId = associationId };

            var response = this._ec2Client.ReplaceNetworkAclAssociation(request);
            this._results = new ActionResults() { Success = true, FocalName = response.NewAssociationId };
        }

        string FindCurrentAssociation()
        {
            var response = this._ec2Client.DescribeNetworkAcls(new DescribeNetworkAclsRequest());

            foreach (var acl in response.NetworkAcls)
            {
                foreach (var association in acl.Associations)
                {
                    if (string.Equals(association.SubnetId, this.Model.SelectedSubnet.SubnetId, StringComparison.InvariantCultureIgnoreCase))
                        return association.NetworkAclAssociationId;
                }
            }

            return null;
        }
    }
}
