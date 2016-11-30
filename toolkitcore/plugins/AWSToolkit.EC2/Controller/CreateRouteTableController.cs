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
    public class CreateRouteTableController
    {
        ActionResults _results;
        CreateRouteTableModel _model;
        IAmazonEC2 _ec2Client;

        public ActionResults Execute(IAmazonEC2 ec2Client)
        {
            this._ec2Client = ec2Client;
            this._model = new CreateRouteTableModel();
            this._model.AvailableVPCs = this.GetListVPCs();

            if (this.Model.AvailableVPCs.Count == 0)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("You must first create a VPC.");
                return new ActionResults().WithSuccess(false);
            }
            this._model.VPC = this._model.AvailableVPCs[0];

            var control = new CreateRouteTableControl(this);
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

        public CreateRouteTableModel Model
        {
            get { return this._model; }
        }

        public void CreateRouteTable()
        {
            var request = new CreateRouteTableRequest
            {
                VpcId = this._model.VPC.VpcId
            };

            var response = this._ec2Client.CreateRouteTable(request);
            this._results = new ActionResults().WithSuccess(true).WithFocalname(response.RouteTable.RouteTableId);
        }
    }
}
