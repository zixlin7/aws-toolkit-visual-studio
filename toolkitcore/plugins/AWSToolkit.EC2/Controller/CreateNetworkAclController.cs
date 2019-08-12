using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class CreateNetworkAclController
    {
        ActionResults _results;
        CreateNetworkAclModel _model;
        IAmazonEC2 _ec2Client;

        public ActionResults Execute(IAmazonEC2 ec2Client)
        {
            this._ec2Client = ec2Client;
            this._model = new CreateNetworkAclModel();
            this._model.AvailableVPCs = this.GetListVPCs();

            if (this.Model.AvailableVPCs.Count == 0)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("You must first create a VPC.");
                return new ActionResults().WithSuccess(false);
            }
            this._model.VPC = this._model.AvailableVPCs[0];

            var control = new CreateNetworkAclControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);

            return this._results;
        }

        List<VPCWrapper> GetListVPCs()
        {
            var vpcs = new List<VPCWrapper>();

            var response = this._ec2Client.DescribeVpcs(new DescribeVpcsRequest());
            response.Vpcs.ForEach(x => vpcs.Add(new VPCWrapper(x)));

            return vpcs;
        }

        public CreateNetworkAclModel Model => this._model;

        public void CreateNetworkAcl()
        {
            var request = new CreateNetworkAclRequest
            {
                VpcId = this._model.VPC.VpcId
            };

            var response = this._ec2Client.CreateNetworkAcl(request);
            this._results = new ActionResults().WithSuccess(true).WithFocalname(response.NetworkAcl.NetworkAclId);
        }
    }
}
