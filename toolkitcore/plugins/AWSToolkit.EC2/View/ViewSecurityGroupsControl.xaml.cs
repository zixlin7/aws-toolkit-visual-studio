using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ViewSecurityGroupsControl.xaml
    /// </summary>
    public partial class ViewSecurityGroupsControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewSecurityGroupsControl));

        const string COLUMN_USERSETTINGS_KEY = "ViewSecurityGroupMainGrid";
        static readonly string DEFAULT_COLUMN_DEFINITIONS;

        static ViewSecurityGroupsControl()
        {
            DEFAULT_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"GroupId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"DisplayName\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"VpcId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"GroupDescription\", \"Type\" : \"Property\"} " +
                "]";
        } 

        ViewSecurityGroupsController _controller;

        public ViewSecurityGroupsControl(ViewSecurityGroupsController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this._ctlIpIngressPermissions.Initialize(this._controller, EC2Constants.PermissionType.Ingress);
            this._ctlIpEgressPermissions.Initialize(this._controller, EC2Constants.PermissionType.Egrees);

            this._ctlDataGrid.Initialize(this._controller.EC2Client, this._controller.Model.PropertyColumnDefinitions, DEFAULT_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
        }

        public override string Title => string.Format("{0} EC2 Security Groups", this._controller.RegionDisplayName);

        public override string UniqueId => "EC2SecurityGroups_" + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.SettingsUniqueKey;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        void onCreateClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var newGroup = this._controller.CreateSecurityGroup();
                if (newGroup != null)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(newGroup);
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error creating security groups: " + e.Message);
            }
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

        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            if (this._controller.Model.SecurityGroups.Count == 0)
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

        void onDeleteSecurityGroupsClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.DeleteSecurityGroups(this._controller.Model.SelectedGroups);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error security groups", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Deleting Security Group", "Error security group: " + e.Message);
            }
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
                IList<SecurityGroupWrapper> selected = this._ctlDataGrid.GetSelectedItems<SecurityGroupWrapper>();

                this._ctlDelete.IsEnabled = selected.Count > 0;
                this._controller.ResetSelection(selected);
                if (selected.Count != 1)
                {
                    this._ctlIpIngressPermissions.DataContext = null;
                    this._ctlIpEgressPermissions.DataContext = null;
                }
                else
                {
                    this._ctlIpIngressPermissions.DataContext = selected[0];
                    this._ctlIpEgressPermissions.DataContext = selected[0];
                    this._ctlIpEgressPermissions.IsEnabled = !string.IsNullOrEmpty(selected[0].VpcId);
                }
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
            string[] tagColumns = this._controller.Model.ListAvailableTags;
            EC2ColumnDefinition[] displayedColumns = this._ctlDataGrid.ColumnDefinitions;

            _columnCustomizationPanel.SetColumnData(fixedAttributes, tagColumns, displayedColumns);
        }
    }
}
