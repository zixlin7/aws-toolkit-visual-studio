using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

using System.Windows.Controls.DataVisualization.Charting;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.CommonUI.Notifications.ToasterPanels;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;
using Amazon.AWSToolkit.ElasticBeanstalk.View.Components;

using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View
{
    /// <summary>
    /// Interaction logic for EnvironmentStatusControl.xaml
    /// </summary>
    public partial class EnvironmentStatusControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(EnvironmentStatusControl));

        const int NON_READY_POLLING = 5000;
        const int READY_POLLING = 15000;

        EnvironmentStatusController _controller;
        bool _hasEverLoadedMetrics = false;
        bool _hasEverLoadedResources = false;
        Timer _pollingTimer = null;
        bool _restartTimerOnNextVisibility = false;

        object syncRoot = new object();
        bool refreshed = false;

        // default off so that env status windows launched outside the scope of
        // the deployment wizard don't start firing toasters
        bool _displayNotificationOnReady = false;
        public bool DisplayNotificationOnReady
        {
            get { return _displayNotificationOnReady; }
            set { _displayNotificationOnReady = value; }
        }

        public EnvironmentStatusControl(EnvironmentStatusController controller)
        {

            InitializeComponent();
            this._controller = controller;
            this._ctlMonitorComponent.Initialize(this._controller);
            this._ctlResourcesComponent.Initialize(this._controller);
            this._ctlEventComponent.Initialize(this._controller);

            this.IsVisibleChanged += this.onIsVisibleChanged;

            this._pollingTimer = new Timer();
            this._pollingTimer.AutoReset = false;
            this._pollingTimer.Elapsed += this.onTimer;

            this._ctlLogsComponent.Initialize(this._controller);

            // apply custom styling to the drop menu; this cannot be done
            // directly via xaml for menus on toolbars
            var menuItemStyle = (Style)this.FindResource("menuItemStyle");
            var menuStyle = (Style)this.environmentMenu.FindResource(ToolBar.MenuStyleKey);
            var baseStyle = (Style)menuStyle.Resources[typeof(MenuItem)];
            menuItemStyle.BasedOn = baseStyle;

            ApplyMenuStyling(environmentMenu, menuItemStyle);

            environmentTypesSubMenu.SubmenuOpened += delegate(object sender, RoutedEventArgs e)
            {
                var envType 
                    = this._controller.Model.ConfigModel.GetValue(BeanstalkConstants.ENVIRONMENT_NAMESPACE, BeanstalkConstants.ENVIRONMENTTYPE_OPTION);
                if (string.IsNullOrEmpty(envType))
                    envType = BeanstalkConstants.EnvType_LoadBalanced;

                foreach (var childItem in environmentTypesSubMenu.Items)
                {
                    var mnu = childItem as MenuItem;
                    if (mnu != null)
                        mnu.IsChecked = envType.Equals(mnu.Tag as string, StringComparison.Ordinal);
                }
            };
        }

        void ApplyMenuStyling(ItemsControl menuItem, Style s)
        {
            if (menuItem == null)
                return;

            if (menuItem.HasItems)
            {
                foreach (var childItem in menuItem.Items)
                {
                    ApplyMenuStyling(childItem as MenuItem, s);
                }
            }

            menuItem.Style = s;
        }

        public override bool SupportsBackGroundDataLoad
        {
            get { return true; }
        }

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            checkIfTerminateAndDisable();
            setupTimerForNextCallback();
            return this._controller.Model;
        }

        public override string Title
        {
            get { return "Env: " + this._controller.Model.EnvironmentName; }
        }

        public override string UniqueId
        {
            get { return "EnvStatus" + this._controller.Model.EnvironmentId; }
        }

        public void CustomizeTabsForEnvironmentType(bool changingEnvironmentType)
        {
            var environmentType 
                = this._controller.Model.ConfigModel.GetValue(BeanstalkConstants.ENVIRONMENT_NAMESPACE, BeanstalkConstants.ENVIRONMENTTYPE_OPTION);
            
            if (string.IsNullOrEmpty(environmentType) || environmentType.Equals(BeanstalkConstants.EnvType_LoadBalanced))
            {
                this._ctlAutoScalingTab.Visibility = Visibility.Visible;
                this._ctlLoadBalancerTab.Visibility = Visibility.Visible;
            }
            else if (environmentType.Equals(BeanstalkConstants.EnvType_SingleInstance))
            {
                this._ctlAutoScalingTab.Visibility = Visibility.Collapsed;
                this._ctlLoadBalancerTab.Visibility = Visibility.Collapsed;
            }

            if (changingEnvironmentType)
            {
                // force tabs to reload; right now we only have one of interest
                this._ctlAdvancedComponent.Rebuild();    
            }
        }

        private void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            refreshed = false;
            lock (syncRoot)
            {
                if (!refreshed)
                {
                    try
                    {
                        this._pollingTimer.Enabled = false;
                        this._controller.Refresh();

                        if (this._ctlMonitorTab.IsSelected)
                            this._ctlMonitorComponent.LoadCloudWatchData();
                        else
                            this._hasEverLoadedMetrics = false;

                        if (this._ctlResourcesTab.IsSelected)
                            this._ctlResourcesComponent.LoadEnviromentResourceData();
                        else
                            this._hasEverLoadedResources = false;

                        checkIfTerminateAndDisable();
                    }
                    catch (Exception e)
                    {
                        LOGGER.Error("Error refreshing environment", e);
                        ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing environment: " + e.Message);
                    }
                    finally
                    {
                        try
                        {
                            System.Threading.Thread.MemoryBarrier();
                            setupTimerForNextCallback();
                        }
                        catch (Exception e)
                        {
                            LOGGER.Error("Failed reseting timer after refreshing.", e);
                        }
                        refreshed = true;
                    }
                }
            }
        }

        void checkIfTerminateAndDisable()
        {
            if (this._controller.Model.Status == BeanstalkConstants.STATUS_TERMINATED ||
                this._controller.Model.Status == BeanstalkConstants.STATUS_TERMINATING)
            {
                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    this._ctlServerComponent.IsEnabled = false;
                    this._ctlLoadBalancerComponent.IsEnabled = false;
                    this._ctlAutoScalingComponent.IsEnabled = false;
                    this._ctlNotificationsComponent.IsEnabled = false;
                    this._ctlContainerComponent.IsEnabled = false;
                    this._ctlAdvancedComponent.IsEnabled = false;

                    this._ctlConnect.IsEnabled = false;
                    this._ctlRestart.IsEnabled = false;
                    this._ctlRebuild.IsEnabled = false;
                    this._ctlTerminate.IsEnabled = false;
                }));
            }
        }

        private void onApplyChangesClick(object sender, RoutedEventArgs e)
        {
            this._controller.ApplyConfigSettings();
        }

        private void onRestartAppServer(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RestartApp();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error restarting app server", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error restarting app server: " + e.Message);
            }

            onRefreshClick(this, null);
        }

        private void onRebuildEnvironment(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RebuildEnvironment();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error rebuilding environment", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error rebuilding environment: " + e.Message);
            }

            onRefreshClick(this, null);
        }

        private void onTerminateEnvironment(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.TerminateEnvironment();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error terminating environment", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error terminating environment: " + e.Message);
            }

            onRefreshClick(this, null);
        }

        private void onConnectToInstance(object sender, RoutedEventArgs e)
        {
            try
            {
                this._controller.ConnectToInstance();
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to connect to instance", ex);
            }
        }
        
        private void onEndPointURLClick(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(this._controller.Model.EndPointURL));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to launch process to go to endpoint", ex);
            }
        }

        // Load CloudWatch data the first time the tab is displayed.
        private void onTabSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (this._ctlMonitorTab.IsSelected && !this._hasEverLoadedMetrics)
            {
                this._hasEverLoadedMetrics = true;
                this._ctlMonitorComponent.LoadCloudWatchData();
            }

            if (this._ctlResourcesTab.IsSelected && !this._hasEverLoadedResources)
            {
                this._hasEverLoadedResources = true;
                this._ctlResourcesComponent.LoadEnviromentResourceData();
            }
        }

        void onTimer(object source, ElapsedEventArgs e)
        {
            refreshed = false; 
            
            lock (syncRoot)
            {
                if (!refreshed)
                {
                    if (!this.IsVisible)
                    {
                        this._restartTimerOnNextVisibility = true;
                        return;
                    } 
                    
                    try
                    {
                        this._controller.Refresh(true);
                        checkIfTerminateAndDisable();
                    }
                    finally
                    {
                        System.Threading.Thread.MemoryBarrier();
                        this.setupTimerForNextCallback();
                        refreshed = true;
                    }
                }
            }
        }


        void onIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible && this._restartTimerOnNextVisibility)
            {
                this._restartTimerOnNextVisibility = false;
                onTimer(this, null);
            }
        }

        void setupTimerForNextCallback()
        {
            if (this._controller.Model.Status == BeanstalkConstants.STATUS_TERMINATED || this._pollingTimer == null)
            {
                return;
            }

            if (this._controller.Model.Status == BeanstalkConstants.STATUS_READY)
                this._pollingTimer.Interval = READY_POLLING;
            else
                this._pollingTimer.Interval = NON_READY_POLLING;

            this._pollingTimer.Enabled = true;
        }

        private void EnvironmentTypeChildMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedEnvType = (sender as MenuItem).Tag as string;
            if (selectedEnvType == null)
                return;

            var currentEnvType = this._controller.Model.ConfigModel.GetValue(BeanstalkConstants.ENVIRONMENT_NAMESPACE, BeanstalkConstants.ENVIRONMENTTYPE_OPTION);
            if (string.IsNullOrEmpty(currentEnvType))
                currentEnvType = BeanstalkConstants.EnvType_LoadBalanced;

            if (!selectedEnvType.Equals(currentEnvType))
                this._controller.ChangeEnvironmentType(selectedEnvType);
        }

        private void environmentTypesSubMenu_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            var allowTypeChange = this._controller.Model.Status == BeanstalkConstants.STATUS_READY;
            _singleInstanceType.IsEnabled = allowTypeChange;
            _loadBalancedType.IsEnabled = allowTypeChange;
            e.Handled = false;
        }
    }
}
