using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class AddNetworkAclRuleController
    {
        IAmazonEC2 _ec2Client;
        string _networkAclId;
        ActionResults _result;
        AddNetworkAclRuleModel _model;
        EC2Constants.PermissionType _permissionType;

        public ActionResults Execute(IAmazonEC2 ec2Client, string networkAclId, EC2Constants.PermissionType permissionType)
        {
            this._ec2Client = ec2Client;
            this._networkAclId = networkAclId;
            this._model = new AddNetworkAclRuleModel();
            this._permissionType = permissionType;

            var control = new AddNetworkAclRuleControl(this);

            if(!ToolkitFactory.Instance.ShellProvider.ShowModal(control))
                return new ActionResults().WithSuccess(false);

            if (this._result == null)
                return new ActionResults().WithSuccess(false);

            return this._result;
        }

        public AddNetworkAclRuleModel Model
        {
            get { return this._model; }
        }

        public void CreateRule()
        {
            if (this._ec2Client == null || this._networkAclId == null)
                return;

            string protocolNumber = ((int)this.Model.IPProtocol.UnderlyingProtocol).ToString();

            CreateNetworkAclEntryRequest request = new CreateNetworkAclEntryRequest()
            {
                NetworkAclId = this._networkAclId,
                Egress = this._permissionType == EC2Constants.PermissionType.Egrees,
                CidrBlock = this.Model.SourceCIDR,
                PortRange = new PortRange() { From = int.Parse(this.Model.PortRangeStart), To = int.Parse(this.Model.PortRangeEnd) },
                RuleNumber = int.Parse(this.Model.RuleNumber),
                RuleAction = this.Model.IsAllow ? "allow" : "deny",
                Protocol = protocolNumber
            };

            this._ec2Client.CreateNetworkAclEntry(request);
            this._result = new ActionResults().WithSuccess(true);
        }
    }
}
