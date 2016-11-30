using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class AttachToVPCController
    {
        ActionResults _results;
        AttachToVPCModel _model;
        IAmazonEC2 _ec2Client;

        public ActionResults Execute(IAmazonEC2 ec2Client, string internetGatewayId)
        {
            this._ec2Client = ec2Client;
            this._model = new AttachToVPCModel(internetGatewayId);
            this._model.AvailableVpcs = this.GetListVpcs();

            if (this.Model.AvailableVpcs.Count == 0)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("You must first create a VPC.");
                return new ActionResults().WithSuccess(false);
            }
            this._model.VPC = this._model.AvailableVpcs[0];

            var control = new AttachToVPCControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);

            return this._results;
        }

        List<VPCWrapper> GetListVpcs()
        {
            var internetGateways = this._ec2Client.DescribeInternetGateways(new DescribeInternetGatewaysRequest()).InternetGateways;
            HashSet<string> attachedVPCs = new HashSet<string>();
            internetGateways.ForEach(i => i.Attachments.ForEach(a => attachedVPCs.Add(a.VpcId)));

            List<VPCWrapper> vpcs = new List<VPCWrapper>();

            var response = this._ec2Client.DescribeVpcs(new DescribeVpcsRequest());
            response.Vpcs.ForEach(x => 
                {
                    if (!attachedVPCs.Contains(x.VpcId))
                        vpcs.Add(new VPCWrapper(x));
                });

            return vpcs;
        }

        public AttachToVPCModel Model
        {
            get { return this._model; }
        }

        public void AttachToVPC()
        {
            var request = new AttachInternetGatewayRequest
            {
                InternetGatewayId = this._model.InternetGatewayId,
                VpcId = this._model.VPC.VpcId
            };

            var response = this._ec2Client.AttachInternetGateway(request);

            this._results = new ActionResults().WithSuccess(true);
        }
    }
}
