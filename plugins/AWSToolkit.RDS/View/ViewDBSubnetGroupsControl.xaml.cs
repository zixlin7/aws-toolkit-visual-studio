using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;
using Amazon.AWSToolkit.RDS.Controller;
using Amazon.AWSToolkit.RDS.Model;
using log4net;

namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for ViewDBSubnetGroupsControl.xaml
    /// </summary>
    public partial class ViewDBSubnetGroupsControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewDBSubnetGroupsControl));

        public ViewDBSubnetGroupsControl()
        {
            InitializeComponent();
        }

        const string COLUMN_USERSETTINGS_KEY = "ViewDBSubnetGroupsControl";
        static readonly string DEFAULT_COLUMN_DEFINITIONS;

        static ViewDBSubnetGroupsControl()
        {
            DEFAULT_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"Name\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Description\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Status\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"VpcId\", \"Type\" : \"Property\"} " +
                "]";
        }

        readonly ViewDBSubnetGroupsController _controller;

        public ViewDBSubnetGroupsControl(ViewDBSubnetGroupsController controller)
        {
            InitializeComponent();
            this._controller = controller;

            this._ctlDataGrid.Initialize(null, this._controller.Model.PropertyColumnDefinitions, DEFAULT_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
        }

        public override object GetInitialData()
        {
            return this._controller.InitialDBSubnetGroupIdentifier;
        }

        public override void RefreshInitialData(object initialData)
        {
            try
            {
                this._controller.RefreshSubnetGroups();
                this.UpdateSelection(initialData as string);
            }
            catch (Exception e)
            {
                var errMsg = string.Format("An error occurred attempting to load the DB subnet groups: {0}", e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", errMsg);
            }
        }

        protected override void PostDataContextBound()
        {
            if (!string.IsNullOrEmpty(this._controller.InitialDBSubnetGroupIdentifier))
                UpdateSelection(this._controller.InitialDBSubnetGroupIdentifier);

        }

        private void UpdateSelection(string subnetGroupIdentifier)
        {
            foreach (var db in this._controller.Model.DBSubnetGroups)
            {
                if (string.Equals(db.DBSubnetGroupIdentifier, subnetGroupIdentifier))
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(db);
                    return;
                }
            }
        }

        public override string Title
        {
            get
            {
                return string.Format("{0} DB Subnet Groups", this._controller.RegionDisplayName);
            }
        }

        public override string UniqueId
        {
            get
            {
                return "RDSDBSubnetGroups_" + this._controller.EndPoint + "_" + this._controller.Account.SettingsUniqueKey;
            }
        }

        public override bool SupportsBackGroundDataLoad
        {
            get
            {
                return true;
            }
        }

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshSubnetGroups();
            }
            catch (Exception e)
            {
                var errMsg = string.Format("An error occurred attempting to load the DB subnet groups: {0}", e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", errMsg);
            }
        }

        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            if (this._ctlDataGrid.SelectedItems.Count == 0)
                return;

            var menu = new ContextMenu();

            var deleteKey = new MenuItem() { Header = "Delete Subnet Group" };
            deleteKey.Click += this.onDeleteClick;
            deleteKey.Icon = IconHelper.GetIcon("delete.png");

            var properties = new MenuItem() { Header = "Properties" };
            properties.Click += this.onPropertiesClick;

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

        void onCreateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this._controller.CreateSubnetGroup();
            }
            catch (Exception exc)
            {
                LOGGER.Error("Error Creating DB Subnet Group", exc);
                var errMsg = string.Format("An error occurred attempting to create the DB subnet group: {0}", exc.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Creating Subnet Group", errMsg);
            }
        }

        void onDeleteClick(object sender, RoutedEventArgs e)
        {
            try
            {
                IList<DBSubnetGroupWrapper> groups = this._ctlDataGrid.GetSelectedItems<DBSubnetGroupWrapper>();
                this._controller.DeleteSubnetGroups(groups);
            }
            catch (Exception exc)
            {
                LOGGER.Error("Error Deleting DB Subnet Group(s)", exc);
                var errMsg = string.Format("An error occurred attempting to delete the DB subnet group(s): {0}", exc.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Deleting Subnet Group", errMsg);
            }
        }

        void onSelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                this.UpdateProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());
                var selected = this._ctlDataGrid.GetSelectedItems<DBSubnetGroupWrapper>();

                this._ctlDelete.IsEnabled = selected.Count > 0;
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
