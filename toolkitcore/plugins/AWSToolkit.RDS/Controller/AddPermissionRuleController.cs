using System.Collections.Generic;
using Amazon.AWSToolkit.Util;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.View;

using Amazon.RDS;
using Amazon.RDS.Model;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.RDS.Controller
{
    public class AddPermissionRuleController
    {
        ActionResults _results;
        AddPermissionRuleModel _model;
        DBSecurityGroupWrapper _dbSecurityGroup;
        IAmazonRDS _rdsClient;
        RDSSecurityGroupRootViewModel _securityRootViewModel;
        private readonly ToolkitContext _toolkitContext;

        public AddPermissionRuleController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public ActionResults Execute(IAmazonRDS rdsClient, DBSecurityGroupWrapper dbSecurityGroup, RDSSecurityGroupRootViewModel securityRootViewModel)
        {
            this._rdsClient = rdsClient;
            this._dbSecurityGroup = dbSecurityGroup;
            this._securityRootViewModel = securityRootViewModel;

            this._model = new AddPermissionRuleModel();
            this._model.CIDR = IPAddressUtil.DetermineIPFromExternalSource() + "/32";
            var control = new AddPermissionRuleControl(this);

            ToolkitFactory.Instance.ShellProvider.ShowModal(control);


            if (this._results != null)
                return this._results;

            return new ActionResults().WithSuccess(false);
        }

        public DBSecurityGroupWrapper SecurityGroup => this._dbSecurityGroup;

        public AddPermissionRuleModel Model => this._model;

        public void AuthorizeRule()
        {
            var request = new AuthorizeDBSecurityGroupIngressRequest();
            request.DBSecurityGroupName = this._dbSecurityGroup.DisplayName;
            if (this.Model.UseCidrIP)
            {
                request.CIDRIP = this.Model.CIDR;
                if (request.CIDRIP.IndexOf('/') == -1)
                    request.CIDRIP += "/32";
            }
            else
            {
                if (string.IsNullOrEmpty(this._dbSecurityGroup.VpcId))
                {
                    request.EC2SecurityGroupOwnerId = stripDisplayNameFromAccountNumber(this.Model.AWSUser);
                    request.EC2SecurityGroupName = this.Model.EC2SecurityGroupName;
                }
                else
                {
                    request.EC2SecurityGroupId = this.Model.EC2SecurityGroupName;
                }
            }

            this._rdsClient.AuthorizeDBSecurityGroupIngress(request);
            this._results = new ActionResults().WithSuccess(true);
        }

        string stripDisplayNameFromAccountNumber(string accountNumber)
        {
            if (accountNumber != null)
            {
                accountNumber = accountNumber.Trim();
                int pos = accountNumber.IndexOf(' ');
                if (pos != -1)
                    accountNumber = accountNumber.Substring(0, pos);
            }

            return accountNumber;
        }

        public List<string> GetEC2SecurityGroups(string accountNumber)
        {
            accountNumber = stripDisplayNameFromAccountNumber(accountNumber);
            List<string> securityGroups = new List<string>();

            var currentAccountNumber = _toolkitContext.ConnectionManager.ActiveAccountId;
            if (string.IsNullOrEmpty(currentAccountNumber) || !accountNumber.Equals(currentAccountNumber))
            {
                return securityGroups;
            }

            var credentialId = _toolkitContext.ConnectionManager.ActiveCredentialIdentifier;
            var client = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(
                credentialId,
                _securityRootViewModel.Region);

            var response = client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest());
            foreach (var group in response.SecurityGroups)
            {
                if (string.IsNullOrEmpty(this._dbSecurityGroup.VpcId) && string.IsNullOrEmpty(group.VpcId))
                    securityGroups.Add(group.GroupName);
                else
                {
                    if(string.Equals(group.VpcId, this._dbSecurityGroup.VpcId))
                        securityGroups.Add(group.GroupId);
                }
            }

            return securityGroups;
        }
    }
}
