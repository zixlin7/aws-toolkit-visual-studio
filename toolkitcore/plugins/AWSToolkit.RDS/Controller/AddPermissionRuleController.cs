using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Util;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

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

        public DBSecurityGroupWrapper SecurityGroup
        {
            get { return this._dbSecurityGroup; }
        }

        public AccountViewModel CurrentAccount
        {
            get{return this._securityRootViewModel.AccountViewModel;}
        }

        public AddPermissionRuleModel Model
        {
            get { return this._model; }
        }

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

            var account = ToolkitFactory.Instance.RootViewModel.AccountFromAccountNumber(accountNumber);
            if (account == null)
                return securityGroups;

            var endPoints = RegionEndPointsManager.GetInstance().GetRegion(this._securityRootViewModel.CurrentEndPoint.RegionSystemName);
            var endPoint = endPoints.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME);

            var config = new AmazonEC2Config() { ServiceURL = endPoint.Url };
            var client = new AmazonEC2Client(account.Credentials, config);

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
