using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for ViewInternetGatewaysControl.xaml
    /// </summary>
    public partial class ViewInternetGatewaysControl : BaseAWSView
    {
       static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewInternetGatewaysControl)); 
        
        const string COLUMN_USERSETTINGS_KEY = "ViewInternetGatewayControlGrid";
        static readonly string DEFAULT_COLUMN_DEFINITIONS;

        static ViewInternetGatewaysControl()
        {
            DEFAULT_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"Name\", \"Type\" : \"Tag\"}, " +
                    "{\"Name\" : \"InternetGatewayId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"State\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"FormattedVPC\", \"Type\" : \"Property\"} " +
                "]";
        }


        ViewInternetGatewayController _controller;
        public ViewInternetGatewaysControl(ViewInternetGatewayController controller)
        {
            InitializeComponent();
            this._controller = controller;

            this._ctlDataGrid.Initialize(this._controller.EC2Client, this._controller.Model.PropertyColumnDefinitions, DEFAULT_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
        }


        public override string Title => String.Format("{0} Internet Gateways", this._controller.RegionDisplayName);

        public override string UniqueId => "InternetGateways_" + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.Identifier.Id;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }
        
         public override void OnEditorOpened(bool success)
         {
             ToolkitFactory.Instance.TelemetryLogger.RecordVpcOpenGateways(new VpcOpenGateways()
             {
                 Result = success ? Result.Succeeded : Result.Failed,
             });
         }
        
        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshInternetGateways();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error Loading Internet Gateways Data: " + e.Message);
            }
        }

        void onCreateInternetGatewayClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                string message = "The Internet gateway is the router on the AWS network that connects your VPC to the Internet.\r\n\r\nDo you want to create an Internet gateway?";
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Create Internet Gateway", message))
                    return;

                var item = this._controller.CreateInternetGateway();
                if (item != null)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(item);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating internet gateway", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Create Error", "Error creating internet gateway: " + e.Message);
            }
        }

        void onDeleteInternetGatewayClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var selectedInternetGateways = this._ctlDataGrid.GetSelectedItems<InternetGatewayWrapper>();
                if(selectedInternetGateways.Count != 1)
                    return;

                if (!string.IsNullOrEmpty(selectedInternetGateways[0].VpcId))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("The Internet Gateway cannot be deleted until all attachments are detached.");
                    return;
                }

                string message = "Are you sure you want to delete the internet gateway?";
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Internet Gateway", message))
                    return;

                this._controller.DeleteInternetGateway(selectedInternetGateways[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting internet gateway", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Delete Error", "Error deleting internet gateway: " + e.Message);
            }
        }

        void onAttachClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var selectedInternetGateways = this._ctlDataGrid.GetSelectedItems<InternetGatewayWrapper>();
                if (selectedInternetGateways.Count != 1)
                    return;

                this._controller.AttachToVPC(selectedInternetGateways[0]);

                var newInstance = this._controller.Model.Gateways.FirstOrDefault(x => x.InternetGatewayId == selectedInternetGateways[0].InternetGatewayId);
                if(newInstance != null)
                    this._ctlDataGrid.SelectAndScrollIntoView(newInstance);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error attaching to vpc: " + e.Message);
            }
        }

        void onDetachClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                string message = "Are you sure you want to detach the internet gateway from the VPC?";
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Detach From VPC", message))
                    return;

                var selectedInternetGateways = this._ctlDataGrid.GetSelectedItems<InternetGatewayWrapper>();
                if (selectedInternetGateways.Count != 1)
                    return;

                this._controller.DetachToVPC(selectedInternetGateways[0]);
                this._ctlDataGrid.SelectAndScrollIntoView(selectedInternetGateways[0]);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error detaching to vpc: " + e.Message);
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
            IList<InternetGatewayWrapper> selectedItems = this._ctlDataGrid.GetSelectedItems<InternetGatewayWrapper>();
            if (selectedItems.Count != 1)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem delete = new MenuItem() { Header = "Delete", Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.internet-gateway_delete.png") };
            delete.Click += this.onDeleteInternetGatewayClick;

            MenuItem attachToVPC = new MenuItem() { Header = "Attach to VPC", Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.internet-gateway_attach-vpc.png") };
            attachToVPC.Click += this.onAttachClick;

            MenuItem detachFromVPC = new MenuItem() { Header = "Detach from VPC", Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.internet-gateway_detach-vpc.png") };
            detachFromVPC.Click += this.onDetachClick;

            MenuItem properties = new MenuItem() { Header = "Properties" };
            properties.Click += this.onPropertiesClick;

            if (string.IsNullOrEmpty(selectedItems[0].VpcId))
                detachFromVPC.IsEnabled = false;
            else
                attachToVPC.IsEnabled = false;

            menu.Items.Add(delete);
            menu.Items.Add(new Separator());
            menu.Items.Add(attachToVPC);
            menu.Items.Add(detachFromVPC);
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

            var selectedInternetGateways = this._ctlDataGrid.GetSelectedItems<InternetGatewayWrapper>();

            this._ctlDelete.IsEnabled = selectedInternetGateways.Count == 1;
            this._ctlAttach.IsEnabled = selectedInternetGateways.Count == 1;
            this._ctlDetach.IsEnabled = selectedInternetGateways.Count == 1;
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
