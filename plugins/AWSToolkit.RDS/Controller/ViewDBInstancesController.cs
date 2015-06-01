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
    public class ViewDBInstancesController : BaseContextCommand, IEventController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewDBInstancesController));

        IAmazonRDS _rdsClient;
        string _endpoint;

        ViewDBInstancesControl _control;
        RDSInstanceRootViewModel _instanceRootViewModel;

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

            this._endpoint = ((IEndPointSupport)this._instanceRootViewModel.Parent).CurrentEndPoint.Url;
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

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
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

        public DBInstanceWrapper LaunchDBInstance()
        {
            var controller = new LaunchDBInstanceController();
            var results = controller.Execute(this._instanceRootViewModel);
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
            return instance;

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

        public void DeleteDBInstance(DBInstanceWrapper instance)
        {
            var controller = new DeleteDBInstanceController();
            controller.Execute(this._rdsClient, this._instanceRootViewModel, instance.DBInstanceIdentifier);
            this.RefreshInstances();
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
            new AddToServerExplorerController().Execute(this._instanceRootViewModel);
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

                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
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

        public AccountViewModel Account
        {
            get { return this._instanceRootViewModel.AccountViewModel; }
        }

        public string EndPoint
        {
            get { return this._instanceRootViewModel.CurrentEndPoint.Url; }
        }

        public string RegionDisplayName
        {
            get
            {
                var region = RegionEndPointsManager.Instance.GetRegion(this._instanceRootViewModel.CurrentEndPoint.RegionSystemName);
                if (region == null)
                    return string.Empty;

                return region.DisplayName;
            }
        }
    }
}
