﻿using System;
using System.Collections.Generic;
using System.Linq;
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
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.RDS.Util;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class ViewDBInstancesController : BaseContextCommand, IEventController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewDBInstancesController));
        private readonly ToolkitContext _toolkitContext;

        IAmazonRDS _rdsClient;

        ViewDBInstancesControl _control;
        RDSInstanceRootViewModel _instanceRootViewModel;

        public ViewDBInstancesController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            if (model is RDSInstanceViewModel)
            {
                this.InitialDBIdentifier = ((RDSInstanceViewModel)model).DBInstance.DBInstanceIdentifier;
                this._instanceRootViewModel = model.FindAncestor<RDSInstanceRootViewModel>();
            }
            else
            {
                this._instanceRootViewModel = model as RDSInstanceRootViewModel;
            }

            if (this._instanceRootViewModel == null)
                return new ActionResults().WithSuccess(false);

            this._rdsClient = this._instanceRootViewModel.RDSClient;
            this.Model = new ViewDBInstancesModel();

            this._control = new ViewDBInstancesControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

            return new ActionResults().WithSuccess(true);
        }

        public string InitialDBIdentifier
        {
            get;
            private set;
        }

        public ViewDBInstancesModel Model
        {
            get;
            private set;
        }

        public void LoadModel()
        {
            RefreshInstances();
        }

        public void RefreshInstances()
        {
            var response = this._rdsClient.DescribeDBInstances();
            List<OptionGroup> allOptions = this._rdsClient.DescribeOptionGroups().OptionGroupsList;

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                this.Model.DBInstances.Clear();
                foreach (var db in response.DBInstances.OrderBy(x => x.DBInstanceIdentifier.ToLower()))
                {
                    var options = new List<OptionGroup>();
                    foreach (var optionMember in db.OptionGroupMemberships)
                    {
                        var option = allOptions.FirstOrDefault(x => x.OptionGroupName == optionMember.OptionGroupName);
                        if (option != null)
                            options.Add(option);
                    }
                    this.Model.DBInstances.Add(new DBInstanceWrapper(db, options));
                }
            }));
        }

        public ActionResults LaunchDBInstance()
        {
            var controller = new LaunchDBInstanceController(_toolkitContext);
            var results = controller.Execute(_instanceRootViewModel);
            /*
             * old code commented is a No-op, needs to be re-evaluated
            if (!results.Success)
                return null;

            // now that we do the launch in an async handler, we won't have this info available
            // right now. Need to come up with some notification scheme...
            DBInstanceWrapper instance = null; /* results.Parameters[LaunchDBInstanceController.CreatedInstanceParameter] as DBInstanceWrapper;
            if (instance != null)
            {
                if(!this.Model.DBInstances.Any(x => x.DBInstanceIdentifier == instance.DBInstanceIdentifier))
                    this.Model.DBInstances.Add(instance);
            }
            */
            return results;

        }

        public void ModifyDBInstance(DBInstanceWrapper instance)
        {
            var controller = new ModifyDBInstanceController();
            controller.Execute(this._instanceRootViewModel, instance);
            this.RefreshInstances();
        }

        public void RebootDBInstances(IList<DBInstanceWrapper> instances)
        {
            var controller = new RebootInstanceController();
            controller.Execute(this._rdsClient, instances);
            this.RefreshInstances();
        }

        public ActionResults DeleteDBInstance(DBInstanceWrapper instance)
        {
            var controller = new DeleteDBInstanceController(_toolkitContext);
            var result = controller.Execute(_rdsClient, _instanceRootViewModel, instance.DBInstanceIdentifier);
            RefreshInstances();
            return result;
        }

        public void CopyEndpointToClipboard(DBInstanceWrapper instance)
        {
            if (instance.IsAvailable)
            {
                string address = instance.Port != null 
                    ? string.Format("{0},{1}", instance.Endpoint, instance.Port) : instance.Endpoint;
                Clipboard.SetText(address);
            }
            else
                ToolkitFactory.Instance.ShellProvider.ShowMessage("Instance Not Yet Available", "The selected instance status must be at 'available' before its address can be obtained.");
        }

        public void TakeSnapshot(DBInstanceWrapper instance)
        {
            var controller = new TakeSnapshotController();
            controller.Execute(this._rdsClient, instance.DBInstanceIdentifier);
        }

        public void CreateSQLServerDatabase(DBInstanceWrapper instance)
        {
            var controller = new CreateSqlServerDBController();
            RDSInstanceViewModel foundViewModel = null;
            foreach (RDSInstanceViewModel viewModel in this._instanceRootViewModel.Children)
            {
                if (string.Equals(viewModel.DBInstance.DBInstanceIdentifier, instance.NativeInstance.DBInstanceIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    foundViewModel = viewModel;
                    break;
                }

            }

            controller.Execute(foundViewModel);
        }

        public void ResetSelection(IList<DBInstanceWrapper> selectedInstances)
        {
            this.Model.SelectedDBInstances.Clear();
            foreach (var item in selectedInstances)
                this.Model.SelectedDBInstances.Add(item);

            ThreadPool.QueueUserWorkItem((WaitCallback)( x => { this.RefreshSelectedEvents(); }));
        }

        public void RegisterDataConnection(DBInstanceWrapper instance)
        {
            var metaModel = this._instanceRootViewModel.MetaNode.FindChild<RDSInstanceViewMetaNode>();
            var model = new RDSInstanceViewModel(metaModel, this._instanceRootViewModel, instance);
            new AddToServerExplorerController().Execute(model);
        }

        public void RefreshSelectedEvents()
        {
            try
            {
                var instances = this.Model.SelectedDBInstances.ToArray();
                var events = new List<Event>();
                foreach (var db in instances)
                {
                    var request = new DescribeEventsRequest()
                    {
                        Duration = 14 * 24 * 60,
                        SourceType = "db-instance",
                        SourceIdentifier = db.DBInstanceIdentifier
                    };

                    var response = this._rdsClient.DescribeEvents(request);
                    events.AddRange(response.Events);
                }

                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    this.Model.SelectedEvents.Clear();
                    foreach (var evnt in events.OrderByDescending(x => x.Date))
                        this.Model.SelectedEvents.Add(evnt);
                }));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error getting events for selected DB Instances", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error getting events for selected DB Instances: " + e.Message);
            }
        }

        public AccountViewModel Account => this._instanceRootViewModel.AccountViewModel;

        public string EndPointUniqueIdentifier => this._instanceRootViewModel.Region.Id;

        public string RegionDisplayName => _instanceRootViewModel.Region.DisplayName;

        public void RecordLaunchDBInstance(ActionResults result)
        {
            var awsConnectionSettings = _instanceRootViewModel?.RDSRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordRdsLaunchInstance(result, awsConnectionSettings);
        }

        public void RecordDeleteDBInstance(ActionResults result)
        {
            var awsConnectionSettings = _instanceRootViewModel?.RDSRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordRdsDeleteInstance(result, awsConnectionSettings);
        }
    }
}
