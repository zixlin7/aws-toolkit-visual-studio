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
    /// Interaction logic for ViewElasticIPsControl.xaml
    /// </summary>
    public partial class ViewElasticIPsControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewElasticIPsControl)); 
        
        const string COLUMN_USERSETTINGS_KEY = "ViewElasticIPsControlGrid";
        static readonly string DEFAULT_COLUMN_DEFINITIONS;

        static ViewElasticIPsControl()
        {
            DEFAULT_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"PublicIp\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"InstanceId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Scope\", \"Type\" : \"Property\"}" +
                "]";
        }

        private readonly ViewElasticIPsController _controller;

        public ViewElasticIPsControl(ViewElasticIPsController controller)
        {
            InitializeComponent();
            this._controller = controller;

            this._ctlDataGrid.Initialize(this._controller.EC2Client, this._controller.Model.PropertyColumnDefinitions, DEFAULT_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
        }

        public override string Title => String.Format("{0} Elastic IPs", this._controller.RegionDisplayName);

        public override string UniqueId => "ElasticIPs_" + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.Identifier.Id;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordEc2OpenElasticIPs(new Ec2OpenElasticIPs()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }
        
        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshElasticIPs();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error Loading Elastic IPs Data: " + e.Message);
            }
        }

        void onDisassociateAddressClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var selectedItems = this._ctlDataGrid.GetSelectedItems<AddressWrapper>();
                if (selectedItems.Count != 1)
                    return;

                string message = "Are you sure you want to disassociate the address?";
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Disassociate Address", message))
                    return;

                this._controller.Disassociate(selectedItems[0]);
                selectAddress(selectedItems[0]);

            }
            catch (Exception e)
            {
                LOGGER.Error("Error disassociate address", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Disassociate Error", "Error disassociating address: " + e.Message);
            }
        }

        void selectAddress(AddressWrapper address)
        {
            foreach (var item in this._controller.Model.Addresses)
            {
                if (item.PublicIp == address.PublicIp)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(item);
                    break;
                }
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
            // todo : try-wrap for caller safety
            IList<AddressWrapper> selectedItems = this._ctlDataGrid.GetSelectedItems<AddressWrapper>();
            if (selectedItems.Count != 1)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem release = new MenuItem() { Header = "Release Address", Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.elastic-ip-release.png") };
            release.CommandParameter = _ctlDataGrid;
            release.Command = _controller.Model.ReleaseElasticIp;

            MenuItem associate = new MenuItem() { Header = "Associate Address", Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.elastic-ip-associate.png") };
            associate.CommandParameter = _ctlDataGrid;
            associate.Command = _controller.Model.AssociateElasticIp;

            MenuItem disassociate = new MenuItem() { Header = "Disassociate Address", Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.elastic-ip-disassociate.png") };
            disassociate.Click += this.onDisassociateAddressClick;

            MenuItem properties = new MenuItem() { Header = "Properties" };
            properties.Click += this.onPropertiesClick;

            if (string.IsNullOrEmpty(selectedItems[0].InstanceId))
                disassociate.IsEnabled = false;

            menu.Items.Add(release);
            menu.Items.Add(new Separator());
            menu.Items.Add(associate);
            menu.Items.Add(disassociate);
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

            var selectedItems = this._ctlDataGrid.GetSelectedItems<AddressWrapper>();
            
            this._ctlDisassociate.IsEnabled = selectedItems.Count == 1 && selectedItems[0].IsAddressInUse;
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
