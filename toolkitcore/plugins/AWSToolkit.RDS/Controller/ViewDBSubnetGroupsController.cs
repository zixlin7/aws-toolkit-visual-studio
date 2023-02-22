using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.EC2.View.DataGrid;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.Util;
using Amazon.AWSToolkit.RDS.View;
using Amazon.RDS;
using log4net;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class ViewDBSubnetGroupsController : BaseContextCommand
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewDBSubnetGroupsController));
        private readonly ToolkitContext _toolkitContext;

        IAmazonRDS _rdsClient;

        ViewDBSubnetGroupsControl _control;
        RDSSubnetGroupsRootViewModel _subnetGroupsRootViewModel;

        public ViewDBSubnetGroupsController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

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

        public ActionResults CreateSubnetGroup(ICustomizeColumnGrid grid)
        {
            var controller = new CreateDBSubnetGroupController(_toolkitContext);
            var results = controller.Execute(_subnetGroupsRootViewModel);
            if (results.Success)
            {
                RefreshSubnetGroups();
                var createdGroup = Model.DBSubnetGroups.FirstOrDefault(sg =>
                  sg.Name == results.FocalName);

                if (createdGroup != null)
                {
                    grid.SelectAndScrollIntoView(createdGroup);
                }
            }

            return results;
        }

        public ActionResults DeleteSubnetGroups(IList<DBSubnetGroupWrapper> groups)
        {
            var controller = new DeleteSubnetGroupController(_toolkitContext, _subnetGroupsRootViewModel);
            var result = controller.Execute(_rdsClient, _subnetGroupsRootViewModel, groups);
            RefreshSubnetGroups();
            return result;
        }


        public AccountViewModel Account => this._subnetGroupsRootViewModel.AccountViewModel;

        public string EndPointUniqueIdentifier => _subnetGroupsRootViewModel.Region.Id;

        public string RegionDisplayName => _subnetGroupsRootViewModel.Region.DisplayName;

        public void RecordCreateSubnetGroup(ActionResults result)
        {
            var awsConnectionSettings = _subnetGroupsRootViewModel?.RDSRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordRdsCreateSubnetGroup(result, awsConnectionSettings);
        }

        public void RecordDeleteSubnetGroup(int count, ActionResults result)
        {
            var awsConnectionSettings = _subnetGroupsRootViewModel?.RDSRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordRdsDeleteSubnetGroup(count, result, awsConnectionSettings);
        }
    }
}
