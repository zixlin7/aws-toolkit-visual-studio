using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.View;

using Amazon.RDS;
using Amazon.RDS.Model;

using log4net;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class ViewDBSecurityGroupsController : BaseContextCommand
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewDBSecurityGroupsController));

        IAmazonRDS _rdsClient;
        string _endpoint;

        ViewDBSecurityGroupsControl _control;
        RDSSecurityGroupRootViewModel _securityRootViewModel;

        public override ActionResults Execute(IViewModel model)
        {
            if (model is RDSSecurityGroupViewModel)
            {
                this.InitialSecurityGroup = ((RDSSecurityGroupViewModel)model).DBGroup.DisplayName;
                this._securityRootViewModel = model.FindAncestor<RDSSecurityGroupRootViewModel>();
            }
            else
            {
                this._securityRootViewModel = model as RDSSecurityGroupRootViewModel;
            }

            if (this._securityRootViewModel == null)
                return new ActionResults().WithSuccess(false);

            this._endpoint = ((IEndPointSupport)this._securityRootViewModel.Parent).CurrentEndPoint.Url;
            this._rdsClient = this._securityRootViewModel.RDSClient;
            this.Model = new ViewDBSecurityGroupsModel();

            this._control = new ViewDBSecurityGroupsControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

            return new ActionResults().WithSuccess(true);
        }

        public string InitialSecurityGroup
        {
            get;
            private set;
        }

        public ViewDBSecurityGroupsModel Model
        {
            get;
            private set;
        }

        public void LoadModel()
        {
            RefreshSecurityGroups();
        }

        public void RefreshSecurityGroups()
        {
            var response = this._rdsClient.DescribeDBSecurityGroups();

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                this.Model.SecurityGroups.Clear();
                foreach (var group in response.DBSecurityGroups.OrderBy(x => x.DBSecurityGroupName.ToLower()))
                {
                    this.Model.SecurityGroups.Add(new DBSecurityGroupWrapper(group));
                }
            }));
        }

        public DBSecurityGroupWrapper CreateSecurityGroup()
        {
            var controller = new CreateSecurityGroupController();
            var results = controller.Execute(this._securityRootViewModel);
            if (results.Success)
            {
                this.RefreshSecurityGroups();

                foreach (var group in this.Model.SecurityGroups)
                {
                    if (group.DisplayName == results.FocalName)
                    {
                        return group;
                    }
                }
            }

            return null;
        }

        public void DeleteSecurityGroups(IList<DBSecurityGroupWrapper> groups)
        {
            var controller = new DeleteSecurityGroupController(this._securityRootViewModel);
            controller.Execute(this._rdsClient, groups);
            this.RefreshSecurityGroups();
        }

        public AccountViewModel Account
        {
            get { return this._securityRootViewModel.AccountViewModel; }
        }

        public string EndPoint
        {
            get { return this._securityRootViewModel.CurrentEndPoint.Url; }
        }

        public string RegionDisplayName
        {
            get
            {
                var region = RegionEndPointsManager.Instance.GetRegion(this._securityRootViewModel.CurrentEndPoint.RegionSystemName);
                if (region == null)
                    return string.Empty;

                return region.DisplayName;
            }
        }

        public void AddPermission(DBSecurityGroupWrapper dbSecurityGroup)
        {
            var controller = new AddPermissionRuleController();
            var results = controller.Execute(this._rdsClient, dbSecurityGroup, this._securityRootViewModel);
            if (results.Success)
            {
                this.RefreshPermissions(dbSecurityGroup);
            }
        }

        public void DeletePermission(DBSecurityGroupWrapper dbSecurityGroup, List<PermissionRule> toBeDeleted)
        {
            foreach (var rule in toBeDeleted)
            {
                var request = new RevokeDBSecurityGroupIngressRequest() { DBSecurityGroupName = dbSecurityGroup.DisplayName };

                request.CIDRIP = rule.CIDR;

                if (!string.IsNullOrEmpty(dbSecurityGroup.VpcId))
                {
                    request.EC2SecurityGroupId = rule.EC2SecurityGroup;
                }
                else
                {
                    request.EC2SecurityGroupOwnerId = rule.AWSUser;
                    request.EC2SecurityGroupName = rule.EC2SecurityGroup;
                }

                this._rdsClient.RevokeDBSecurityGroupIngress(request);
            }

            this.RefreshPermissions(dbSecurityGroup);
        }

        public void RefreshPermissions(DBSecurityGroupWrapper dbSecurityGroup)
        {
            var response = this._rdsClient.DescribeDBSecurityGroups(new DescribeDBSecurityGroupsRequest() { DBSecurityGroupName = dbSecurityGroup.DisplayName });

            if (response.DBSecurityGroups.Count == 1)
            {
                dbSecurityGroup.RefreshNative(response.DBSecurityGroups[0]);
            }
        }
    }
}
