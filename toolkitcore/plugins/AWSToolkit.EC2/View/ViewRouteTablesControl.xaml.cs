using System;
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
    /// Interaction logic for ViewRouteTablesControl.xaml
    /// </summary>
    public partial class ViewRouteTablesControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewRouteTablesControl));

        const string COLUMN_USERSETTINGS_KEY = "ViewRouteTableControlGrid";
        static readonly string DEFAULT_COLUMN_DEFINITIONS;

        static ViewRouteTablesControl()
        {
            DEFAULT_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"Name\", \"Type\" : \"Tag\"}, " +
                    "{\"Name\" : \"RouteTableId\", \"Type\" : \"Property\"}, " +
//                    "{\"Name\" : \"AssociatedWith\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"IsMain\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"VpcId\", \"Type\" : \"Property\"} " +
                "]";
        }


        ViewRouteTablesController _controller;
        public ViewRouteTablesControl(ViewRouteTablesController controller)
        {
            InitializeComponent();
            this._controller = controller;

            this._ctlDataGrid.Initialize(this._controller.EC2Client, this._controller.Model.PropertyColumnDefinitions, DEFAULT_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);

            this._ctlRoutes.Initialize(this._controller);
            this._ctlAssociations.Initialize(this._controller);
        }


        public override string Title => String.Format("{0} Route Tables", this._controller.RegionDisplayName);

        public override string UniqueId => "RouteTables_" + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.SettingsUniqueKey;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshRouteTables();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error Loading Route Tables Data: " + e.Message);
            }
        }

        void onSetMainRouteTableClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var selectedRouteTables = this._ctlDataGrid.GetSelectedItems<RouteTableWrapper>();
                if (selectedRouteTables.Count != 1)
                    return;

                string message = string.Format("Are you sure you want to set route table {0} as the Main route table?", selectedRouteTables[0].RouteTableId);
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Set Main Route Table", message))
                    return;

                var item = this._controller.SetMainRouteTable(selectedRouteTables[0]);
                if (item != null)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(item);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error setting main route table route table", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Set Error", "Error setting main route table route table: " + e.Message);
            }
        }

        void onCreateRouteTableClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var item = this._controller.CreateRouteTable();
                if (item != null)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(item);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating route table", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Create Error", "Error creating route table: " + e.Message);
            }
        }

        void onDeleteRouteTableClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var selectedRouteTables = this._ctlDataGrid.GetSelectedItems<RouteTableWrapper>();
                if (selectedRouteTables.Count != 1)
                    return;

                string message = "Are you sure you want to delete this Route Table?";
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Route Table", message))
                    return;

                this._controller.DeleteRouteTable(selectedRouteTables[0]);
                this._controller.Model.RouteTables.Remove(selectedRouteTables[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting route table", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Delete Error", "Error deleting route table: " + e.Message);
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
            var selectedItems = this._ctlDataGrid.GetSelectedItems<RouteTableWrapper>();
            if (selectedItems.Count != 1)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem delete = new MenuItem() { Header = "Delete", Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.route-table-remove.png") };
            delete.Click += this.onDeleteRouteTableClick;

            MenuItem setMain = new MenuItem() { Header = "Set as Main Table"};
            setMain.Click += this.onSetMainRouteTableClick;

            MenuItem properties = new MenuItem() { Header = "Properties" };
            properties.Click += this.onPropertiesClick;

            delete.IsEnabled = selectedItems[0].CanDelete;
            setMain.IsEnabled = !selectedItems[0].HasMainAssociation;

            menu.Items.Add(delete);
            menu.Items.Add(setMain);
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

            var selectedItems = this._ctlDataGrid.GetSelectedItems<RouteTableWrapper>();

            if (selectedItems.Count == 1)
            {
                this._ctlDelete.IsEnabled = selectedItems[0].CanDelete;
                this._ctlRoutes.DataContext = selectedItems[0];
                this._ctlAssociations.DataContext = selectedItems[0];
            }
            else
            {
                this._ctlDelete.IsEnabled = false;
                this._ctlRoutes.DataContext = null;
                this._ctlAssociations.DataContext = null;
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
