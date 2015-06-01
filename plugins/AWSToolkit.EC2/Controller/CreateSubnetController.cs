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
    public class CreateSubnetController
    {
        ActionResults _results;
        CreateSubnetModel _model;
        IAmazonEC2 _ec2Client;

        public ActionResults Execute(IAmazonEC2 ec2Client)
        {
            this._ec2Client = ec2Client;
            this._model = new CreateSubnetModel();

            this._model.AllVPCs = new List<VPCWrapper>();
            var vpcResponse = this._ec2Client.DescribeVpcs(new DescribeVpcsRequest());
            vpcResponse.Vpcs.ForEach(x => this._model.AllVPCs.Add(new VPCWrapper(x)));

            if (this.Model.AllVPCs.Count == 0)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("You must first create a VPC.");
                return new ActionResults().WithSuccess(false);
            }

            this._model.AllAvailabilityZones = new List<string>();
            this._model.AllAvailabilityZones.Add(CreateSubnetModel.NO_PREFERENCE_ZONE);
            var zoneResponse = this._ec2Client.DescribeAvailabilityZones(new DescribeAvailabilityZonesRequest());
            zoneResponse.AvailabilityZones.ForEach(x => this._model.AllAvailabilityZones.Add(x.ZoneName));

            var control = new CreateSubnetControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);

            return this._results;
        }

        public CreateSubnetModel Model
        {
            get { return this._model; }
        }

        public void CreateSubnet()
        {
            var request = new CreateSubnetRequest()
            {
                CidrBlock = this._model.CIDRBlock,
                VpcId = this._model.VPC.VpcId
            };

            if (this._model.AvailabilityZone != CreateSubnetModel.NO_PREFERENCE_ZONE)
                request.AvailabilityZone = this._model.AvailabilityZone;

            var response = this._ec2Client.CreateSubnet(request);

            this._results = new ActionResults()
                .WithFocalname(response.Subnet.SubnetId)
                .WithSuccess(true);
        }
    }
}
