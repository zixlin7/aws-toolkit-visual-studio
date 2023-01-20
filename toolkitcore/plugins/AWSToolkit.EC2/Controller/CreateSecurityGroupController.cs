using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class CreateSecurityGroupController
    {
        ActionResults _results;
        CreateSecurityGroupModel _model;
        IAmazonEC2 _ec2Client;

        public ActionResults Execute(IAmazonEC2 ec2Client)
        {
            this._ec2Client = ec2Client;
            this._model = new CreateSecurityGroupModel();

            this._model.AvailableVPCs = this.GetListVPCs();
            this._model.IsVpcOnlyEnvironment = EC2Utilities.CheckForVpcOnlyMode(ec2Client);

            if (!this._model.IsVpcOnlyEnvironment)
                this._model.AvailableVPCs.Insert(0, new VPCWrapper(new Vpc { VpcId = VPCWrapper.NotInVpcPseudoId }));

            var control = new CreateSecurityGroupControl(this);
            if (!ToolkitFactory.Instance.ShellProvider.ShowModal(control))
            {
                return ActionResults.CreateCancelled();
            }

            return _results ?? ActionResults.CreateFailed();
        }

        List<VPCWrapper> GetListVPCs()
        {
            var vpcs = new List<VPCWrapper>();

            var response = this._ec2Client.DescribeVpcs(new DescribeVpcsRequest());
            response.Vpcs.ForEach(x => vpcs.Add(new VPCWrapper(x)));

            return vpcs;
        }

        public CreateSecurityGroupModel Model => this._model;

        public void CreateSecurityGroup()
        {
            var request = new CreateSecurityGroupRequest()
            {
                GroupName = this.Model.Name,
                Description = this.Model.Description
            };

            if (this._model.SelectedVPC.VpcId.StartsWith("vpc-"))
                request.VpcId = this._model.SelectedVPC.VpcId;

            var response = this._ec2Client.CreateSecurityGroup(request);

            this._results = new ActionResults()
                .WithFocalname(response.GroupId)
                .WithSuccess(true);
        }
    }
}
