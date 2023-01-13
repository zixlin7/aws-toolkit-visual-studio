using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ViewInstancesControl.xaml
    /// </summary>
    public partial class ViewInstancesControl : BaseAWSView
    {
        

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewInstancesControl));

        const string COLUMN_USERSETTINGS_KEY = "ViewInstancesControlGrid";
        static readonly string DEFAULT_INSTANCES_COLUMN_DEFINITIONS;        

        static ViewInstancesControl()
        {
            DEFAULT_INSTANCES_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"Name\", \"Type\" : \"Tag\"}, " +
                    "{\"Name\" : \"InstanceId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Status\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"ImageId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"RootDeviceType\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"InstanceType\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"FormattedSecurityGroups\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"AvailabilityZone\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"LaunchTime\", \"Type\" : \"Property\"} " +
                "]";
        }     


        ViewInstancesController _controller;
        public ViewInstancesControl(ViewInstancesController controller)
        {
            InitializeComponent();
            this._controller = controller;

            this._ctlInstanceVolumes.Initialize(this._controller);
            this._ctlDataGrid.Initialize(this._controller.EC2Client, this._controller.Model.InstancePropertyColumnDefinitions, DEFAULT_INSTANCES_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
        }

        public override string Title => string.Format("{0} EC2 Instances", this._controller.RegionDisplayName);

        public override string UniqueId => "SecurityGroups: " + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.Identifier.Id;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordEc2OpenInstances(new Ec2OpenInstances()
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
                LOGGER.Error("Error refreshing", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing instance: " + e.Message);
            }
        }

        void onLaunchClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                IList<RunningInstanceWrapper> newInstances = this._controller.LaunchInstance();
                if (newInstances != null && newInstances.Count > 0)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(newInstances[0]);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error launching instance", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error launching EC2 instance: " + e.Message);
            }
        }

        void onGotFocus(object sender, RoutedEventArgs e)
        {
            this.UpdateProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());
        }

        void onInstanceSelectionChanged(object sender, RoutedEventArgs evnt)
        {
            this.UpdateProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());

            List<RunningInstanceWrapper> selectedInstances = new List<RunningInstanceWrapper>();
            foreach (RunningInstanceWrapper instance in this._ctlDataGrid.SelectedItems)
            {
                selectedInstances.Add(instance);
            }

            this._controller.UpdateSelection(selectedInstances);

            this._ctlDelete.IsEnabled = false;
            var instances = getSelectedItemsAsList(true);
            foreach (var instance in instances)
            {
                if (instance.NativeInstance.State.Name == EC2Constants.INSTANCE_STATE_RUNNING ||
                    instance.NativeInstance.State.Name == EC2Constants.INSTANCE_STATE_STOPPED)
                {
                    this._ctlDelete.IsEnabled = true;
                    break;
                }
            }
        }

        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            List<RunningInstanceWrapper> selectedItems = getSelectedItemsAsList(true);
            if (selectedItems.Count == 0)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem getPassword = createMenuItem("Get Windows Passwords", this.onGetPasswordClick);
            MenuItem openRemoteDesktop = createMenuItem("Open Remote Desktop", this.onOpenRemoteDesktopClick);
            MenuItem ssh = createMenuItem("Open SSH Session", this.onOpenSSHSessionClick);
            MenuItem scp = createMenuItem("Open SCP Session", this.onOpenSCPSessionClick);

            MenuItem getConsoleOutput = CreateMenuItem("Get System Log", _controller.Model.ViewSystemLog, _ctlDataGrid);

            MenuItem createImage = CreateMenuItem("Create Image (EBS AMI)", _controller.Model.CreateImage, _ctlDataGrid);
            MenuItem changeTerminationProtection = CreateMenuItem("Change Termination Protection", _controller.Model.ChangeTerminationProtection, _ctlDataGrid);
            MenuItem changeInstanceType = createMenuItem("Change Instance Type", this.onChangeInstanceType);
            MenuItem changeShutdownBehavior = createMenuItem("Change Shutdown Behavior", this.onChangeShutdownBehavior);
            MenuItem changeUserData = createMenuItem("View/Change User Data", this.onChangeUserData);

            MenuItem terminate = createMenuItem("Terminate", this.onTerminateClick);
            MenuItem reboot = createMenuItem("Reboot", this.onRebootClick);
            MenuItem stop = createMenuItem("Stop", this.onStopClick);
            MenuItem start = createMenuItem("Start", this.onStartClick);

            MenuItem associateElasticIP = createMenuItem("Associate Elastic IP", this.onAssociatingElasticIP);
            MenuItem disassociateElasticIP = createMenuItem("Disassociate Elastic IP", this.onDisassociateElasticIP);

            MenuItem properties = new MenuItem() { Header = "Properties" };
            properties.Click += this.onPropertiesClick;



            if (selectedItems.Count > 1)
            {
                getPassword.IsEnabled = false;
                openRemoteDesktop.IsEnabled = false;
                ssh.IsEnabled = false;
                changeInstanceType.IsEnabled = false;
                changeShutdownBehavior.IsEnabled = false;
                changeUserData.IsEnabled = false;
                associateElasticIP.IsEnabled = false;
                disassociateElasticIP.IsEnabled = false;
            }

            if (!EC2Constants.INSTANCE_STATE_STOPPED.Equals(selectedItems[0].NativeInstance.State.Name))
            {
                changeInstanceType.IsEnabled = false;
            }

            if (EC2Constants.INSTANCE_STATE_RUNNING.Equals(selectedItems[0].NativeInstance.State.Name))
            {
                if (!selectedItems[0].IsWindowsPlatform)
                {
                    getPassword.IsEnabled = false;
                    openRemoteDesktop.IsEnabled = false;
                }
                else
                {
                    ssh.IsEnabled = false;
                    scp.IsEnabled = false;
                }

                if (string.IsNullOrEmpty(selectedItems[0].ElasticIPAddress))
                    disassociateElasticIP.IsEnabled = false;
                else
                    associateElasticIP.IsEnabled = false;
            }
            else
            {
                getPassword.IsEnabled = false;
                openRemoteDesktop.IsEnabled = false;
                ssh.IsEnabled = false;
                scp.IsEnabled = false;
            }

            if (getPassword.IsEnabled)
            {
                menu.Items.Add(getPassword);
                menu.Items.Add(openRemoteDesktop);
            }
            else if (ssh.IsEnabled)
            {
                menu.Items.Add(ssh);
                menu.Items.Add(scp);                
            }

            menu.Items.Add(getConsoleOutput);
            menu.Items.Add(new Separator());
            menu.Items.Add(createImage);
            menu.Items.Add(changeTerminationProtection);
            menu.Items.Add(changeUserData);
            menu.Items.Add(changeInstanceType);
            menu.Items.Add(changeShutdownBehavior);
            menu.Items.Add(new Separator());
            menu.Items.Add(associateElasticIP);
            menu.Items.Add(disassociateElasticIP);
            menu.Items.Add(new Separator());
            menu.Items.Add(terminate);
            menu.Items.Add(reboot);
            menu.Items.Add(stop);
            menu.Items.Add(start);
            menu.Items.Add(new Separator());
            menu.Items.Add(properties);


            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        MenuItem createMenuItem(string header, RoutedEventHandler handler)
        {
            MenuItem mi = new MenuItem() { Header = header};
            mi.Click += handler;
            return mi;
        }

        MenuItem CreateMenuItem(string header, ICommand command, object commandParameter)
        {
            return new MenuItem { Header = header, CommandParameter = commandParameter, Command = command };
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

        void onOpenSSHSessionClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var instances = getSelectedItemsAsList(true);
                if (instances.Count != 1)
                    return;

                this._controller.OpenSSHSession(instances[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error opening ssh session", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error opening ssh session: " + e.Message);
            }
        }

        void onOpenSCPSessionClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var instances = getSelectedItemsAsList(true);
                if (instances.Count != 1)
                    return;

                this._controller.OpenSCPSession(instances[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error opening scp session", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error opening scp session: " + e.Message);
            }
        }

        void onOpenRemoteDesktopClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var instances = getSelectedItemsAsList(true);
                if (instances.Count != 1)
                    return;

                this._controller.OpenRemoteDesktop(instances[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error opening remote desktop", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error opening remote desktop: " + e.Message);
            }
        }

        void onGetPasswordClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var instances = getSelectedItemsAsList(true);
                if (instances.Count != 1)
                    return;

                this._controller.GetPassword(instances[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error getting password", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error getting password: " + e.Message);
            }
        }

        void onChangeInstanceType(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var instances = getSelectedItemsAsList(true);
                if (instances.Count != 1)
                    return;

                this._controller.ChangeInstanceType(instances[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error changing instance types", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error changing instance type: " + e.Message);
            }
        }

        void onAssociatingElasticIP(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var instances = getSelectedItemsAsList(true);
                if (instances.Count != 1)
                    return;

                var instance = this._controller.AssociatingElasticIP(instances[0]);
                if(instance != null)
                    this._ctlDataGrid.SelectAndScrollIntoView(instance);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error associating Elastic IP address", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error associating Elastic IP address: " + e.Message);
            }
        }

        void onDisassociateElasticIP(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var instances = getSelectedItemsAsList(true);
                if (instances.Count != 1)
                    return;

                string message = string.Format("Are you sure you want to disassociate the address {0}?", instances[0].ElasticIPAddress);
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Disassociate Address", message))
                    return;

                var instance = this._controller.DisassociateElasticIP(instances[0]);
                if (instance != null)
                    this._ctlDataGrid.SelectAndScrollIntoView(instance);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error disassociating Elastic IP address", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error disassociating Elastic IP address: " + e.Message);
            }
        }

        void onChangeShutdownBehavior(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var instances = getSelectedItemsAsList(true);
                if (instances.Count != 1)
                    return;

                this._controller.ChangeShutdownBehavior(instances[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error changing shutdown behavior", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error changing shutdown behavior: " + e.Message);
            }
        }

        void onChangeUserData(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var instances = getSelectedItemsAsList(true);
                if (instances.Count != 1)
                    return;

                this._controller.ChangeUserData(instances[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error changing user data", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error changing user data: " + e.Message);
            }
        }

        void onTerminateClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.TerminateInstances(getSelectedItemsAsList(true));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error terminating instances", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error terminating instances: " + e.Message);
            }
        }

        void onRebootClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RebootInstances(getSelectedItemsAsList(true));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error rebooting instances", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error rebooting instances: " + e.Message);
            }
        }

        void onStopClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.StopInstances(getSelectedItemsAsList(true));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error stopping instances", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error stopping instances: " + e.Message);
            }
        }

        void onStartClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.StartInstances(getSelectedItemsAsList(true));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error starting instances", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error starting instances: " + e.Message);
            }
        }

        List<RunningInstanceWrapper> getSelectedItemsAsList(bool ignoreTerminated)
        {
            var items = new List<RunningInstanceWrapper>();
            foreach (RunningInstanceWrapper selectedItem in this._ctlDataGrid.SelectedItems)
            {
                if (ignoreTerminated && selectedItem.IsTerminated())
                {
                    continue;
                }
                items.Add(selectedItem);
            }

            return items;
        }

        void OnColumnCustomizationApplyPressed(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("OnColumnCustomizationApplyPressed");

            this._ctlDataGrid.ColumnDefinitions = _columnCustomizationPanel.SelectedColumns;
        }

        private void OnColumnCustomizationDropPanelOpening(object sender, RoutedEventArgs e)
        {
            EC2ColumnDefinition[] fixedAttributes = this._controller.Model.InstancePropertyColumnDefinitions;
            string[] tagColumns = this._controller.Model.ListInstanceAvailableTags;
            EC2ColumnDefinition[] displayedColumns = this._ctlDataGrid.ColumnDefinitions;

            _columnCustomizationPanel.SetColumnData(fixedAttributes, tagColumns, displayedColumns);
        }
    }
}
