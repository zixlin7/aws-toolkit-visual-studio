using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ViewSubnetsControl.xaml
    /// </summary>
    public partial class ViewSubnetsControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewSubnetsControl)); 
        
        const string COLUMN_USERSETTINGS_KEY = "ViewSubnetControlGrid";
        static readonly string DEFAULT_COLUMN_DEFINITIONS;

        static ViewSubnetsControl()
        {
            DEFAULT_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"Name\", \"Type\" : \"Tag\"}, " +
                    "{\"Name\" : \"SubnetId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"SubnetState\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"VpcId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"CidrBlock\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"FormattedAvailableIpAddressCount\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"AvailabilityZone\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"RouteTableId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"NetworkAclId\", \"Type\" : \"Property\"} " +
                "]";
        }


        ViewSubnetsController _controller;
        public ViewSubnetsControl(ViewSubnetsController controller)
        {
            InitializeComponent();
            this._controller = controller;

            this._ctlDataGrid.Initialize(this._controller.EC2Client, this._controller.Model.PropertyColumnDefinitions, DEFAULT_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
        }


        public override string Title => String.Format("{0} Subnets", this._controller.RegionDisplayName);

        public override string UniqueId => "Subnets_" + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.SettingsUniqueKey;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordVpcOpenSubnets(new VpcOpenSubnets()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshSubnets();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error Loading Subnets Data: " + e.Message);
            }
        }

        void onCreateClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var item = this._controller.CreateSubnet();
                if (item != null)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(item);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating subnet", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Create Error", "Error creating subnet: " + e.Message);
            }
        }

        void onDeleteClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var selectedSubnets = this._ctlDataGrid.GetSelectedItems<SubnetWrapper>();
                if (selectedSubnets.Count != 1)
                    return;

  

                string message = "Are you sure you want to delete the subnet?";
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Subnet", message))
                    return;

                this._controller.DeleteSubnet(selectedSubnets[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting subnet", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Delete Error", "Error deleting subnet: " + e.Message);
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

        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            IList<SubnetWrapper> selectedItems = this._ctlDataGrid.GetSelectedItems<SubnetWrapper>();
            if (selectedItems.Count != 1)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem delete = new MenuItem() { Header = "Delete", Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.subnet-remove.png") };
            delete.Click += this.onDeleteClick;

            MenuItem properties = new MenuItem() { Header = "Properties" };
            properties.Click += this.onPropertiesClick;

            menu.Items.Add(delete);
            menu.Items.Add(properties);

            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        void onGotFocus(object sender, RoutedEventArgs e)
        {
            this.UpdateProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());
        }

        void onSelectionChanged(object sender, RoutedEventArgs e)
        {
            this.UpdateProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());

            var selectedItems = this._ctlDataGrid.GetSelectedItems<SubnetWrapper>();

            this._ctlDelete.IsEnabled = selectedItems.Count == 1;
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
