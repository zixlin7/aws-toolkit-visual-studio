using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.View;
using Amazon.RDS;
using log4net;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class ViewDBSubnetGroupsController : BaseContextCommand
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewDBSubnetGroupsController));

        IAmazonRDS _rdsClient;

        ViewDBSubnetGroupsControl _control;
        RDSSubnetGroupsRootViewModel _subnetGroupsRootViewModel;

        public override ActionResults Execute(Navigator.Node.IViewModel model)
        {
            if (model is RDSSubnetGroupViewModel)
            {
                this.InitialDBSubnetGroupIdentifier = ((RDSSubnetGroupViewModel)model).SubnetGroup.DBSubnetGroupIdentifier;
                this._subnetGroupsRootViewModel = model.FindAncestor<RDSSubnetGroupsRootViewModel>();
            }
            else
            {
                this._subnetGroupsRootViewModel = model as RDSSubnetGroupsRootViewModel;
            }

            if (this._subnetGroupsRootViewModel == null)
                return new ActionResults().WithSuccess(false);

            this._rdsClient = this._subnetGroupsRootViewModel.RDSClient;
            this.Model = new ViewDBSubnetGroupsModel();

            this._control = new ViewDBSubnetGroupsControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

            return new ActionResults().WithSuccess(true);
        }

        public string InitialDBSubnetGroupIdentifier
        {
            get;
            private set;
        }

        public ViewDBSubnetGroupsModel Model
        {
            get;
            private set;
        }

        public void LoadModel()
        {
            RefreshSubnetGroups();
        }

        public void RefreshSubnetGroups()
        {
            var response = this._rdsClient.DescribeDBSubnetGroups();

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                this.Model.DBSubnetGroups.Clear();
                foreach (var db in response.DBSubnetGroups.OrderBy(x => x.DBSubnetGroupName.ToLower()))
                {
                    this.Model.DBSubnetGroups.Add(new DBSubnetGroupWrapper(db));
                }
            }));
        }

        public DBSubnetGroupWrapper CreateSubnetGroup()
        {
            var controller = new CreateDBSubnetGroupController();
            var results = controller.Execute(this._subnetGroupsRootViewModel);
            if (results.Success)
            {
                this.RefreshSubnetGroups();

                foreach (var group in this.Model.DBSubnetGroups)
                {
                    if (group.Name == results.FocalName)
                    {
                        return group;
                    }
                }
            }

            return null;
        }

        public void DeleteSubnetGroups(IList<DBSubnetGroupWrapper> groups)
        {
            var controller = new DeleteSubnetGroupController(this._subnetGroupsRootViewModel);
            controller.Execute(this._rdsClient, this._subnetGroupsRootViewModel, groups);
            this.RefreshSubnetGroups();
        }


        public AccountViewModel Account => this._subnetGroupsRootViewModel.AccountViewModel;

        public string EndPointUniqueIdentifier => _subnetGroupsRootViewModel.Region.Id;

        public string RegionDisplayName => _subnetGroupsRootViewModel.Region.DisplayName;
    }
}
