using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.RDS.Controller;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;


namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for ViewDBInstancesControl.xaml
    /// </summary>
    public partial class ViewDBInstancesControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewDBInstancesControl));

        const string COLUMN_USERSETTINGS_KEY = "ViewDBInstancesControl";
        static readonly string DEFAULT_COLUMN_DEFINITIONS;

        static ViewDBInstancesControl()
        {
            DEFAULT_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"DBInstanceIdentifier\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"MultiAZ\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"DBInstanceClass\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"DBInstanceStatus\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"FormattedSecurityGroups\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Engine\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"AvailabilityZone\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"PendingValues\", \"Type\" : \"Property\"} " +
                "]";
        }

        ViewDBInstancesController _controller;

        public ViewDBInstancesControl(ViewDBInstancesController controller)
        {
            InitializeComponent();
            this._controller = controller;

            this._ctlDataGrid.Initialize(null, this._controller.Model.PropertyColumnDefinitions, DEFAULT_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);

            this._ctlEventViewer.Initialize(this._controller);
            this._ctlEventViewer.DataContext = this._controller.Model;
            
        }

        public override object GetInitialData()
        {
            return this._controller.InitialDBIdentifier;
        }

        public override void RefreshInitialData(object initialData)
        {
            try
            {
                this._controller.RefreshInstances();
                this.UpdateSelection(initialData as string);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error loading db instances: " + e.Message);
            }
        }

        protected override void PostDataContextBound()
        {
            if (!string.IsNullOrEmpty(this._controller.InitialDBIdentifier))
                UpdateSelection(this._controller.InitialDBIdentifier);

        }

        private void UpdateSelection(string dbIdentifier)
        {
            foreach (var db in this._controller.Model.DBInstances)
            {
                if (string.Equals(db.DBInstanceIdentifier, dbIdentifier))
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(db);
                    return;
                }
            }
        }

        public override string Title => string.Format("{0} DB Instances", this._controller.RegionDisplayName);

        public override string UniqueId => "RDSDBInstances_" + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.Identifier.Id;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordRdsOpenInstances(new RdsOpenInstances()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshInstances();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error loading db instances: " + e.Message);
            }
        }

        void onRegisterDataConnection(object sender, RoutedEventArgs evnt)
        {
            try
            {
                IList<DBInstanceWrapper> instances = this._ctlDataGrid.GetSelectedItems<DBInstanceWrapper>();
                if (instances.Count != 1)
                    throw new Exception("Register DB Instances only supports one instance at a time.");

                this._controller.RegisterDataConnection(instances[0]);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error loading instances: " + e.Message);
            }
        }

        void onLaunchClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var newGroup = this._controller.LaunchDBInstance();
                if (newGroup != null)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(newGroup);
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error creating instance: " + e.Message);
            }
        }

        void onDeleteDBInstanceClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                IList<DBInstanceWrapper> instances = this._ctlDataGrid.GetSelectedItems<DBInstanceWrapper>();
                if (instances.Count != 1)
                    throw new Exception("Deleting DB Instances only supports one instance at a time.");

                this._controller.DeleteDBInstance(instances[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting db instance", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Deleting DB Instance", "Error deleting db instance: " + e.Message);
            }
        }

        void onModifyDBInstanceClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                IList<DBInstanceWrapper> instances = this._ctlDataGrid.GetSelectedItems<DBInstanceWrapper>();
                if (instances.Count != 1)
                    throw new Exception("Modifying DB Instances only supports one instance at a time.");
                this._controller.ModifyDBInstance(instances[0]);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error modifying instance: " + e.Message);
            }
        }

        void onCopySQLServerEndpointClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                IList<DBInstanceWrapper> instances = this._ctlDataGrid.GetSelectedItems<DBInstanceWrapper>();
                if (instances.Count != 1)
                    throw new Exception("Copying endpoint only supports one instance at a time.");

                this._controller.CopyEndpointToClipboard(instances[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error copying db instance endpoint", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Copying DB Instance Endpoint", "Error copying db instance endpoint: " + e.Message);
            }
        }

        void onTakeSnapshotClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                IList<DBInstanceWrapper> instances = this._ctlDataGrid.GetSelectedItems<DBInstanceWrapper>();
                if (instances.Count != 1)
                    throw new Exception("Taking DB Instances only supports one instance at a time.");

                this._controller.TakeSnapshot(instances[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error taking snapshot", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Taking Snapshot", "Error taking snapshot: " + e.Message);
            }
        }

        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            if (this._ctlDataGrid.SelectedItems.Count == 0)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem registerKey = new MenuItem() { Header = "Add to Server Explorer..." };
            registerKey.Click += this.onRegisterDataConnection;
            registerKey.IsEnabled = this._ctlDataGrid.SelectedItems.Count == 1 &&
                ((DBInstanceWrapper)this._ctlDataGrid.SelectedItem).DatabaseType != DatabaseTypes.Unknown &&
                ((DBInstanceWrapper)this._ctlDataGrid.SelectedItem).DatabaseType != DatabaseTypes.Oracle;

            MenuItem createSQLServerDBKey = null;
            MenuItem copySQLServerEndPointKey = null;
            if (((DBInstanceWrapper)this._ctlDataGrid.SelectedItem).DatabaseType == DatabaseTypes.SQLServer)
            {
                createSQLServerDBKey = new MenuItem() { Header = "Create SQL Server Database..." };
                createSQLServerDBKey.Click += this.onCreateSQLServerDatabaseClick;
                createSQLServerDBKey.IsEnabled = this._ctlDataGrid.SelectedItems.Count == 1;

                copySQLServerEndPointKey = new MenuItem() { Header = "Copy Address to Clipboard" };
                copySQLServerEndPointKey.Click += this.onCopySQLServerEndpointClick;
                copySQLServerEndPointKey.Icon = IconHelper.GetIcon("Amazon.AWSToolkit.Resources.copy.png");
                IList<DBInstanceWrapper> instances = this._ctlDataGrid.GetSelectedItems<DBInstanceWrapper>();
                copySQLServerEndPointKey.IsEnabled = instances.Count == 1 && instances[0].IsAvailable;
            }

            MenuItem modifyKey = new MenuItem() { Header = "Modify DB Instance..." };
            modifyKey.Click += this.onModifyDBInstanceClick;
            modifyKey.Icon = IconHelper.GetIcon("Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.ModifyDBInstance.png");
            modifyKey.IsEnabled = this._ctlDataGrid.SelectedItems.Count == 1;

            MenuItem deleteKey = new MenuItem() { Header = "Delete DB Instance" };
            deleteKey.Click += this.onDeleteDBInstanceClick;
            deleteKey.Icon = IconHelper.GetIcon("delete.png");
            deleteKey.IsEnabled = this._ctlDataGrid.SelectedItems.Count == 1;

            MenuItem snapshotKey = new MenuItem() { Header = "Take Snapshot..." };
            snapshotKey.Click += this.onTakeSnapshotClick;
            snapshotKey.Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.TakeSnapshot.png");
            snapshotKey.IsEnabled = this._ctlDataGrid.SelectedItems.Count == 1;

            MenuItem rebootKey = new MenuItem() { Header = "Reboot" };
            rebootKey.Click += this.onRebootDBInstancesClick;

            MenuItem properties = new MenuItem() { Header = "Properties" };
            properties.Click += this.onPropertiesClick;

            if (ToolkitFactory.Instance.ShellProvider.QueryShellProviderService<IRegisterDataConnectionService>() != null)
                menu.Items.Add(registerKey);
            if (createSQLServerDBKey != null)
                menu.Items.Add(createSQLServerDBKey);
            if (copySQLServerEndPointKey != null)
                menu.Items.Add(copySQLServerEndPointKey);

            if (menu.Items.Count != 0)
                menu.Items.Add(new Separator());

            menu.Items.Add(modifyKey);
            menu.Items.Add(snapshotKey);
            menu.Items.Add(rebootKey);
            menu.Items.Add(new Separator());
            menu.Items.Add(deleteKey);
            menu.Items.Add(new Separator());
            menu.Items.Add(properties);

            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }


        void onPropertiesClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this.ShowProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());
            }
            catch (Exception e)
            {
                LOGGER.Error("Error displaying properties", e);
            }
        }

        void onRebootDBInstancesClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                IList<DBInstanceWrapper> instances = this._ctlDataGrid.GetSelectedItems<DBInstanceWrapper>();
                this._controller.RebootDBInstances(instances);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error Rebooting DBInstance(s)", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Rebooting DBInstance(s)", "Error Rebooting DBInstance(s): " + e.Message);
            }
        }

        void onCreateSQLServerDatabaseClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                IList<DBInstanceWrapper> instances = this._ctlDataGrid.GetSelectedItems<DBInstanceWrapper>();
                this._controller.CreateSQLServerDatabase(instances[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error Creating SQL Server Database", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Creating SQL Server Database", "Error Creating SQL Server Database: " + e.Message);
            }
        }

        void onSelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                this.UpdateProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());
                IList<DBInstanceWrapper> selected = this._ctlDataGrid.GetSelectedItems<DBInstanceWrapper>();

                this._ctlDelete.IsEnabled = selected.Count == 1;
                this._controller.ResetSelection(selected);
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error updating selection", ex);
            }
        }

        void OnColumnCustomizationApplyPressed(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("OnColumnCustomizationApplyPressed");

            this._ctlDataGrid.ColumnDefinitions = _columnCustomizationPanel.SelectedColumns;
        }

        private void OnColumnCustomizationDropPanelOpening(object sender, RoutedEventArgs e)
        {
            EC2ColumnDefinition[] fixedAttributes = this._controller.Model.PropertyColumnDefinitions;
            EC2ColumnDefinition[] displayedColumns = this._ctlDataGrid.ColumnDefinitions;

            _columnCustomizationPanel.SetColumnData(fixedAttributes, null, displayedColumns);
        }
    }
}
