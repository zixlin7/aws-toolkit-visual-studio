using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ViewNetworkAclsControl.xaml
    /// </summary>
    public partial class ViewNetworkAclsControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewNetworkAclsControl));

        const string COLUMN_USERSETTINGS_KEY = "ViewNetworkAclControlGrid";
        static readonly string DEFAULT_COLUMN_DEFINITIONS;

        static ViewNetworkAclsControl()
        {
            DEFAULT_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"Name\", \"Type\" : \"Tag\"}, " +
                    "{\"Name\" : \"NetworkAclId\", \"Type\" : \"Property\"}, " +
//                    "{\"Name\" : \"AssociatedWith\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"FormattedDefault\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"VpcId\", \"Type\" : \"Property\"} " +
                "]";
        }


        ViewNetworkAclsController _controller;
        public ViewNetworkAclsControl(ViewNetworkAclsController controller)
        {
            InitializeComponent();
            this._controller = controller;

            this._ctlDataGrid.Initialize(this._controller.EC2Client, this._controller.Model.PropertyColumnDefinitions, DEFAULT_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
            this._ctlAssociations.Initialize(this._controller);
            this._ctlInbound.Initialize(this._controller, EC2Constants.PermissionType.Ingress);
            this._ctlOutbound.Initialize(this._controller, EC2Constants.PermissionType.Egrees);
        }


        public override string Title
        {
            get
            {
                return String.Format("{0} Network Acls", this._controller.RegionDisplayName);
            }
        }

        public override string UniqueId
        {
            get
            {
                return "NetworkAcls_" + this._controller.EndPoint + "_" + this._controller.Account.SettingsUniqueKey;
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
                this._controller.RefreshNetworkAcls();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error Loading Network Acls Data: " + e.Message);
            }
        }

        void onCreateNetworkAclClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var item = this._controller.CreateNetworkAcl();
                if (item != null)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(item);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating network acl", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Create Error", "Error creating network acl: " + e.Message);
            }
        }

        void onDeleteNetworkAclClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var selectedNetworkAcls = this._ctlDataGrid.GetSelectedItems<NetworkAclWrapper>();
                if (selectedNetworkAcls.Count != 1)
                    return;

                string message = "Are you sure you want to delete this Network Acl?";
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Network Acl", message))
                    return;

                this._controller.DeleteNetworkAcl(selectedNetworkAcls[0]);
                this._controller.Model.NetworkAcls.Remove(selectedNetworkAcls[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting network acl", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Delete Error", "Error deleting network acl: " + e.Message);
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
            var selectedItems = this._ctlDataGrid.GetSelectedItems<NetworkAclWrapper>();
            if (selectedItems.Count != 1)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem delete = new MenuItem() { Header = "Delete", Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.networkacl-remove.png") };
            delete.Click += this.onDeleteNetworkAclClick;

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

            var selectedItems = this._ctlDataGrid.GetSelectedItems<NetworkAclWrapper>();

            if (selectedItems.Count == 1)
            {
                this._ctlDelete.IsEnabled = true;

                this._ctlAssociations.DataContext = selectedItems[0];
                this._ctlInbound.DataContext = selectedItems[0];
                this._ctlOutbound.DataContext = selectedItems[0];
            }
            else
            {
                this._ctlDelete.IsEnabled = false;

                this._ctlAssociations.DataContext = null;
                this._ctlInbound.DataContext = null;
                this._ctlOutbound.DataContext = null;
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
