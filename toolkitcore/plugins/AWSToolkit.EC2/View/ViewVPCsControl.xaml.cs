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
    /// Interaction logic for ViewVPCsControl.xaml
    /// </summary>
    public partial class ViewVPCsControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewVPCsControl)); 
        
        const string COLUMN_USERSETTINGS_KEY = "ViewVPCsControlGrid";
        static readonly string DEFAULT_COLUMN_DEFINITIONS;

        static ViewVPCsControl()
        {
            DEFAULT_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"Name\", \"Type\" : \"Tag\"}, " +
                    "{\"Name\" : \"VpcId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"VpcState\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"CidrBlock\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"FormattedIsDefault\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"DhcpOptionsId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"InstanceTenancy\", \"Type\" : \"Property\"} " +
                "]";
        }     


        ViewVPCsController _controller;
        public ViewVPCsControl(ViewVPCsController controller)
        {
            InitializeComponent();
            this._controller = controller;

            this._ctlDataGrid.Initialize(this._controller.EC2Client, this._controller.Model.VPCPropertyColumnDefinitions, DEFAULT_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
        }


        public override string Title
        {
            get
            {
                return String.Format("{0} VPCs", this._controller.RegionDisplayName);
            }
        }

        public override string UniqueId
        {
            get
            {
                return "VPCs_" + this._controller.EndPoint + "_" + this._controller.Account.SettingsUniqueKey;
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
                this._controller.RefreshVPCs();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error Loading VPCs Data: " + e.Message);
            }
        }

        void onCreateVPCClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var vpc = this._controller.CreateVPC();
                if (vpc != null)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(vpc);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating vpc", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Create Error", "Error creating vpc: " + e.Message);
            }
        }

        void onAssociateDHCPOptionsSetClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var selectedVpcs = this._ctlDataGrid.GetSelectedItems<VPCWrapper>();
                if (selectedVpcs.Count != 1)
                    return;

                var vpc = this._controller.AssociateDHCPOptionsSet(selectedVpcs[0]);
                if (vpc != null)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(vpc);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error associating DHCP options set to the VPC", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Associate Error", "Error associating DHCP options set to the VPC: " + e.Message);
            }
        }

        void onDNSSettingsClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var selectedVpcs = this._ctlDataGrid.GetSelectedItems<VPCWrapper>();
                if (selectedVpcs.Count != 1)
                    return;


                this._controller.UpdateDNSSettings(selectedVpcs[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error updating DNS options for the VPC", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("DNS Settings Error", "Error updating the DNS settings for the VPC: " + e.Message);
            }
        }

        void onDeleteVPCClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var selectedVpcs = this._ctlDataGrid.GetSelectedItems<VPCWrapper>();
                if(selectedVpcs.Count != 1)
                    return;

                this._controller.DeleteVPC(selectedVpcs[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting vpc", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Delete Error", "Error deleting vpc: " + e.Message);
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
            var selectedItems = this._ctlDataGrid.GetSelectedItems<VPCWrapper>();
            if (selectedItems.Count != 1)
                return;

            var menu = new ContextMenu();

            var associateDHCP = new MenuItem { Header = "Associate DHCP Options Set"};
            associateDHCP.Click += this.onAssociateDHCPOptionsSetClick;

            var dnsOptions = new MenuItem {Header = "DNS Settings..."};
            dnsOptions.Click += this.onDNSSettingsClick;

            var delete = new MenuItem { Header = "Delete", Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.vpc-delete.png") };
            delete.Click += this.onDeleteVPCClick;

            var properties = new MenuItem { Header = "Properties" };
            properties.Click += this.onPropertiesClick;

            menu.Items.Add(delete);
            menu.Items.Add(new Separator());
            menu.Items.Add(associateDHCP);
            menu.Items.Add(dnsOptions);
            menu.Items.Add(new Separator());
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

            var selectedVpcs = this._ctlDataGrid.GetSelectedItems<VPCWrapper>();
            this._ctlDelete.IsEnabled = selectedVpcs.Count == 1;
        }

        void OnColumnCustomizationApplyPressed(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("OnColumnCustomizationApplyPressed");

            this._ctlDataGrid.ColumnDefinitions = _columnCustomizationPanel.SelectedColumns;
        }

        private void OnColumnCustomizationDropPanelOpening(object sender, RoutedEventArgs e)
        {
            EC2ColumnDefinition[] fixedAttributes = this._controller.Model.VPCPropertyColumnDefinitions;
            string[] tagColumns = this._controller.Model.ListVPCAvailableTags;
            EC2ColumnDefinition[] displayedColumns = this._ctlDataGrid.ColumnDefinitions;

            _columnCustomizationPanel.SetColumnData(fixedAttributes, tagColumns, displayedColumns);
        }
    }
}
