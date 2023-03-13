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
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for ViewDBSecurityGroupsControl.xaml
    /// </summary>
    public partial class ViewDBSecurityGroupsControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewDBSecurityGroupsControl));

        const string COLUMN_USERSETTINGS_KEY = "ViewDBSecurityGroupsControl";
        static readonly string DEFAULT_COLUMN_DEFINITIONS;

        static ViewDBSecurityGroupsControl()
        {
            DEFAULT_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"DisplayName\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Description\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"FormattedIPRanges\", \"Type\" : \"Property\"} " +
                "]";
        }

        ViewDBSecurityGroupsController _controller;

        public ViewDBSecurityGroupsControl(ViewDBSecurityGroupsController controller)
        {
            InitializeComponent();
            this._controller = controller;

            this._ctlDataGrid.Initialize(null, this._controller.Model.PropertyColumnDefinitions, DEFAULT_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
            this._ctlPermissions.Initialize(this._controller);
        }

        public override object GetInitialData()
        {
            return this._controller.InitialSecurityGroup;
        }

        public override void RefreshInitialData(object initialData)
        {
            try
            {
                this._controller.RefreshSecurityGroups();
                this.UpdateSelection(initialData as string);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error loading db security groups: " + e.Message);
            }
        }

        protected override void PostDataContextBound()
        {
            if (!string.IsNullOrEmpty(this._controller.InitialSecurityGroup))
                UpdateSelection(this._controller.InitialSecurityGroup);

        }

        private void UpdateSelection(string dbIdentifier)
        {
            foreach (var db in this._controller.Model.SecurityGroups)
            {
                if (string.Equals(db.DisplayName, dbIdentifier))
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(db);
                    return;
                }
            }
        }

        public override string Title => string.Format("{0} DB Security Groups", this._controller.RegionDisplayName);

        public override string UniqueId => "RDSDBSecurityGroups_" + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.Identifier.Id;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordRdsOpenSecurityGroups(new RdsOpenSecurityGroups()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshSecurityGroups();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error loading security groups: " + e.Message);
            }
        }

        void onCreateClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var result = _controller.CreateSecurityGroup(_ctlDataGrid);
                _controller.RecordCreateSecurityGroup(result);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error creating security groups: " + e.Message);
                _controller.RecordCreateSecurityGroup(ActionResults.CreateFailed(e));
            }
        }

        void onDeleteSecurityGroupsClick(object sender, RoutedEventArgs evnt)
        {
            var count = 0;

            try
            {
                var groups = _ctlDataGrid.GetSelectedItems<DBSecurityGroupWrapper>();
                count = groups.Count;
                var result = _controller.DeleteSecurityGroups(groups);
                _controller.RecordDeleteSecurityGroup(count, result);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error security groups", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Deleting Security Group", "Error security group: " + e.Message);
                _controller.RecordDeleteSecurityGroup(count, ActionResults.CreateFailed(e));
            }
        }

        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            if (this._ctlDataGrid.SelectedItems.Count == 0)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem deleteKey = new MenuItem() { Header = "Delete Security Group" };
            deleteKey.Click += this.onDeleteSecurityGroupsClick;
            deleteKey.Icon = IconHelper.GetIcon("delete.png");

            MenuItem properties = new MenuItem() { Header = "Properties" };
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

        void onSelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                this.UpdateProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());
                IList<DBSecurityGroupWrapper> selected = this._ctlDataGrid.GetSelectedItems<DBSecurityGroupWrapper>();

                this._ctlDelete.IsEnabled = selected.Count > 0;
                if (selected.Count != 1)
                    this._ctlPermissions.DataContext = null;
                else
                    this._ctlPermissions.DataContext = selected[0];
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
