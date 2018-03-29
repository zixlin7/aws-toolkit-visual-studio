using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
using Amazon.AWSToolkit.EC2.Utils;
using Amazon.AWSToolkit.EC2.View.DataGrid;

using Amazon.Runtime.Internal.Settings;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ViewImagesControl.xaml
    /// </summary>
    public partial class ViewAMIsControl : BaseAWSView
    {
        const string COLUMN_USERSETTINGS_KEY = "ViewAMIMainGrid";

        static readonly string DEFAULT_COLUMN_DEFINITIONS;
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewInstancesControl));

        ViewAMIsController _controller;
        Guid _lastTextFilterChangeToken;

        static ViewAMIsControl()
        {
            DEFAULT_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"ImageId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Name\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Description\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"FormattedOwner\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"FormattedVisibility\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"State\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"FormattedPlatform\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"RootDeviceType\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"VirtualizationType\", \"Type\" : \"Property\"} " +
                "]";
        }                                                    

        public ViewAMIsControl(ViewAMIsController controller)
        {
            InitializeComponent();            

            this._controller = controller;
            this._ctlCommonFilters.ItemsSource = CommonImageFilters.AllFilters;
            this._ctlPlatformFilters.ItemsSource = PlatformPicker.AllPlatforms;

            this._ctlDataGrid.Initialize(this._controller.EC2Client, this._controller.Model.PropertyColumnDefinitions, DEFAULT_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
            updateSearchColumns();
        }

        void updateSearchColumns()
        {
            this._controller.SetSearchColumns(this._ctlDataGrid.Columns);
        }

        public override string Title
        {
            get
            {
                return string.Format("{0} EC2 AMIs", this._controller.RegionDisplayName);
            }
        }

        public override string UniqueId
        {
            get
            {
                return "AMIs: " + this._controller.EndPoint + "_" + this._controller.Account.SettingsUniqueKey;
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
            displayWaitState();
            try
            {
                this._controller.LoadModel();
                return this._controller.Model;
            }
            finally
            {
                clearWaitState();
            }
        }

        void buildColumns()
        {

        }

        void onGridContextMenu(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._ctlDataGrid.SelectedItems.Count == 0)
                    return;

                ContextMenu menu = new ContextMenu();

                MenuItem deregisterImage = new MenuItem() { Header = "De-register AMI" };
                deregisterImage.Click += this.onDeregisterClick;
                deregisterImage.Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.deregisterami.png");

                MenuItem copyAmi = new MenuItem() { Header = "Copy to Region" };                
                foreach (var item in RegionEndPointsManager.GetInstance().Regions)
                {                    
                    if (_controller.RegionSystemName.Equals(item.SystemName) || item.HasRestrictions)
                    {
                        // Skip the current region and the regions with restrictions.
                        continue;
                    }

                    if (item.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME) == null)
                    {
                        // Skip the region if it does not have a EC2 service.
                        continue;
                    }

                    var regionItem = new MenuItem { Header = item.DisplayName, Tag = item };
                    regionItem.Click += this.onCopyAmiClick;
                    regionItem.Icon = IconHelper.GetIcon(item.FlagIcon);

                    copyAmi.Items.Add(regionItem);
                }
 
                MenuItem editPermission = new MenuItem() { Header = "Edit Permission" };
                editPermission.Click += this.onEditPermissionClick;
                editPermission.Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.ami_permissions.png");

                MenuItem launchInstance = new MenuItem() { Header = "Launch Instance" };
                launchInstance.Click += this.onLaunchInstanceClick;
                launchInstance.Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.launch-instance.png");

                MenuItem properties = new MenuItem() { Header = "Properties" };
                properties.Click += this.onPropertiesClick;

                editPermission.IsEnabled = isEditPermissionEnabled();
                launchInstance.IsEnabled = isLaunchInstancesEnabled();
                deregisterImage.IsEnabled = isDeregisterEnabled();
                copyAmi.IsEnabled = IsCopyAmiEnabled();

                menu.Items.Add(launchInstance);
                menu.Items.Add(editPermission);
                menu.Items.Add(new Separator());
                menu.Items.Add(copyAmi);
                menu.Items.Add(new Separator());
                menu.Items.Add(deregisterImage);
                menu.Items.Add(new Separator());
                menu.Items.Add(properties);

                menu.PlacementTarget = this;
                menu.IsOpen = true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error with context menu", e);
            }
        }

        bool isLaunchInstancesEnabled()
        {
            return this._ctlDataGrid.SelectedItems.Count == 1;
        }

        bool isEditPermissionEnabled()
        {
            if (this._ctlDataGrid.SelectedItems.Count != 1)
                return false;

            
            return this._controller.Model.CommonImageFilter == CommonImageFilters.OWNED_BY_ME;
        }

        bool isDeregisterEnabled()
        {
            if (this._ctlDataGrid.SelectedItems.Count == 0)
                return false;

            return this._controller.Model.CommonImageFilter == CommonImageFilters.OWNED_BY_ME;
        }

        bool IsCopyAmiEnabled()
        {
            var selectedImages = this._ctlDataGrid.GetSelectedItems<ImageWrapper>();
            if (selectedImages.Count==1 && selectedImages[0].State.Equals("AVAILABLE",StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        void onCopyAmiClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var sourceRegion = (sender as Control).Tag as RegionEndPointsManager.RegionEndPoints;
                this._controller.CopyAmi(this._ctlDataGrid.GetSelectedItems<ImageWrapper>()[0],sourceRegion);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error copying image(s)", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error copying image(s): " + e.Message);
            }
        }

        void onDeregisterClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Deregister(this._ctlDataGrid.GetSelectedItems<ImageWrapper>());
            }
            catch (Exception e)
            {
                LOGGER.Error("Error de-registering image(s)", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error de-registering image(s): " + e.Message);
            }
        }

        void onEditPermissionClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._ctlDataGrid.SelectedItems.Count != 1)
                    return;

                var image = this._ctlDataGrid.SelectedItem as ImageWrapper;
                this._controller.EditPermission(image);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error editting permissions image(s)", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error editting permissions image(s): " + e.Message);
            }
        }

        void onLaunchInstanceClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._ctlDataGrid.SelectedItems.Count != 1)
                    return;

                var image = this._ctlDataGrid.SelectedItem as ImageWrapper;
                this._controller.LaunchInstance(image);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error launching instances", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error launching instances: " + e.Message);
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
                ToolkitFactory.Instance.ShellProvider.ShowError("Error displaying properties: " + e.Message);
            }
        }

        void onSelectionChanged(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this.UpdateProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());
                this._ctlDeregister.IsEnabled = isDeregisterEnabled();
                this._ctlLaunchInstance.IsEnabled = isLaunchInstancesEnabled();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error with selection change", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error with selection change: " + e.Message);
            }
        }

        void onLoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            // This is a check so we don't get a second load when the DataContext
            // is set
            if (!this.IsEnabled)
                return;

            try
            {
                ThreadPool.QueueUserWorkItem(this.asyncRefresh,
                   new LoadState(true, true));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing images: " + e.Message);
            }
        }

        void onFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            // This is a check so we don't get a second load when the DataContext
            // is set
            if (!this.IsEnabled)
                return;

            ThreadPool.QueueUserWorkItem(this.asyncRefresh,
               new LoadState(true, false));

            // Only allow tag editing on AMI owned by the user
            this._ctlDataGrid.IsReadOnly = this._controller.Model.CommonImageFilter != CommonImageFilters.OWNED_BY_ME;
        }
        
        void onTextFilterChange(object sender, TextChangedEventArgs e)
        {
            // This is a check so we don't get a second load when the DataContext
            // is set
            if (!this.IsEnabled)
                return;

            this._lastTextFilterChangeToken = Guid.NewGuid();
            ThreadPool.QueueUserWorkItem(this.asyncRefresh, 
                new LoadState(this._lastTextFilterChangeToken, false, false));
        }

        void asyncRefresh(object state)
        {
            if (!(state is LoadState))
                return;
            LoadState loadState = (LoadState)state;
            if (loadState.DisplayWaitState)
                displayWaitState();

            try
            {
                if (loadState.LastTextFilterChangeToken != Guid.Empty)
                    Thread.Sleep(Constants.TEXT_FILTER_IDLE_TIMER);

                if (loadState.LastTextFilterChangeToken == Guid.Empty || this._lastTextFilterChangeToken == loadState.LastTextFilterChangeToken)
                    this._controller.RefreshImages(loadState.FullRefresh);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing images: " + e.Message);
            }
            finally
            {
                if(loadState.DisplayWaitState)
                    clearWaitState();
            }
        }

        void displayWaitState()
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.IsEnabled = false;
                    this._ctlLoadingMessage.Text = LoadState.LOADING_TEXT;
                }));
        }

        void clearWaitState()
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                this.IsEnabled = true;
                this._ctlLoadingMessage.Text = "";
            }));
        }

        void OnColumnCustomizationApplyPressed(object sender, RoutedEventArgs e)
        {
            this._ctlDataGrid.ColumnDefinitions = _columnCustomizationPanel.SelectedColumns;
            updateSearchColumns();
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
