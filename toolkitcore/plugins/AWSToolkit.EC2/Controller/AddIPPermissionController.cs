using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Nodes;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class AddIPPermissionController
    {
        FeatureViewModel _viewModel;
        IAmazonEC2 _ec2Client;
        SecurityGroup _group;
        ActionResults _result;
        AddIPPermissionModel _model;
        EC2Constants.PermissionType _permissionType;

        public ActionResults Execute(FeatureViewModel viewModel, SecurityGroup group, EC2Constants.PermissionType permissionType)
        {
            this._viewModel = viewModel;
            this._ec2Client = this._viewModel.EC2Client;
            this._group = group;
            this._model = new AddIPPermissionModel();
            this._permissionType = permissionType;

            AddIPPermissionControl control = new AddIPPermissionControl(this);

            if(!ToolkitFactory.Instance.ShellProvider.ShowModal(control))
                return new ActionResults().WithSuccess(false);

            if (this._result == null)
                return new ActionResults().WithSuccess(false);

            return this._result;
        }

        public AddIPPermissionModel Model
        {
            get { return this._model; }
        }

        public void CreateIPPermission()
        {
            if (this._ec2Client == null || this._group == null)
                return;


            IpPermission permission = new IpPermission();
            permission.IpProtocol = this.Model.IPProtocol.UnderlyingProtocol.ToString().ToLower();
            permission.FromPort = int.Parse(this.Model.PortRangeStart);
            permission.ToPort = int.Parse(this.Model.PortRangeEnd);

            if (this.Model.IsPortAndIpChecked)
            {
                permission.IpRanges = new List<string>(){this.Model.SourceCIDR};
            }
            else
            {
                UserIdGroupPair pair = new UserIdGroupPair() { UserId = stripDisplayNameFromAccountNumber(this.Model.UserId) };
                if (!string.IsNullOrEmpty(this._group.VpcId))
                    pair.GroupId = this.Model.GroupName;
                else
                    pair.GroupName = this.Model.GroupName;

                permission.UserIdGroupPairs = new List<UserIdGroupPair>() { pair };
            }

            if (this._permissionType == EC2Constants.PermissionType.Ingress)
            {
                var request = new AuthorizeSecurityGroupIngressRequest();
                request.GroupId = this._group.GroupId;
                request.IpPermissions.Add(permission);

                this._ec2Client.AuthorizeSecurityGroupIngress(request);
            }
            else
            {
                var request = new AuthorizeSecurityGroupEgressRequest();
                request.GroupId = this._group.GroupId;
                request.IpPermissions.Add(permission);

                this._ec2Client.AuthorizeSecurityGroupEgress(request);
            }

            this._result = new ActionResults().WithSuccess(true);
        }

        public AccountViewModel CurrentAccount
        {
            get
            {
                return this._viewModel.AccountViewModel;
            }
        }

        public List<string> GetEC2SecurityGroups(string accountNumber)
        {
            accountNumber = stripDisplayNameFromAccountNumber(accountNumber);
            List<string> securityGroups = new List<string>();

            var account = ToolkitFactory.Instance.RootViewModel.AccountFromAccountNumber(accountNumber);
            if (account == null)
                return securityGroups;

            var endPoints = RegionEndPointsManager.Instance.GetRegion(this._viewModel.RegionSystemName);
            var endPoint = endPoints.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME);

            var config = new AmazonEC2Config() { ServiceURL = endPoint.Url };
            if (endPoint.Signer != null)
                config.SignatureVersion = endPoint.Signer;
            if (endPoint.AuthRegion != null)
                config.AuthenticationRegion = endPoint.AuthRegion;

            var client = new AmazonEC2Client(account.Credentials, config);

            var response = client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest());
            foreach (var group in response.SecurityGroups)
            {
                // filter out 'self' references to group we're editing if we're listing
                // groups owned by our initial account
                if (string.IsNullOrEmpty(this._group.VpcId))
                {
                    if (CurrentAccount.AccountNumber.Equals(accountNumber) 
                            && group.GroupName.Equals(this._group.GroupName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    securityGroups.Add(group.GroupName);
                }
                else if (string.Equals(group.VpcId, this._group.VpcId))
                {
                    if (CurrentAccount.AccountNumber.Equals(accountNumber)
                            && group.GroupId.Equals(this._group.GroupId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    securityGroups.Add(group.GroupId);
                }
            }

            return securityGroups;
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
    }
}
