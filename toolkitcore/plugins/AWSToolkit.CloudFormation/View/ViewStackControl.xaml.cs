using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CloudFormation.Controllers;
using Amazon.AWSToolkit.CloudFormation.View.Components;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.View
{
    /// <summary>
    /// Interaction logic for ViewStackControl.xaml
    /// </summary>
    public partial class ViewStackControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewStackControl));

        const int LOCK_WAIT = 5000;
        const int NON_READY_POLLING = 5000;
        const int READY_POLLING = 30000;

        System.Timers.Timer _pollingTimer = null;
        ViewStackController _controller;

        bool _restartTimerOnNextVisibility = false;

        object syncRoot = new object();
        bool refreshed = false;
        bool _isResourcesLoaded = false;
        bool _isMetricsLoaded = false;


        public ViewStackControl(ViewStackController controller)
        {
            this._controller = controller;
            InitializeComponent();

            this._ctlStackEvents.Initialize(this._controller);
            this._ctlStackResources.Initialize(this._controller);
            this._ctlStackMonitoring.Initialize(this._controller);

            this._pollingTimer = new System.Timers.Timer();
            this._pollingTimer.AutoReset = false;
            this._pollingTimer.Elapsed += this.onTimer;

            this.DataContextChanged += onDataContextChanged;
            this.setDataContextOfVSToolkitControls(controller.Model);

            this.Unloaded += new RoutedEventHandler(onUnloaded);
        }

        void onUnloaded(object sender, RoutedEventArgs e)
        {
            if (this._pollingTimer != null)
            {
                this._pollingTimer.Enabled = false;
                this._pollingTimer.Dispose();
                this._pollingTimer = null;
            }
        }

        internal ViewStackController Controller => this._controller;

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            setDataContextOfVSToolkitControls(this.DataContext);
        }

        void setDataContextOfVSToolkitControls(object dataContext)
        {
            this._ctlLabelApplicationURL.DataContext = dataContext;
            this._ctlApplicationURL.DataContext = dataContext;

            this._ctlLabelServerlessApplicationURL.DataContext = dataContext;
            this._ctlServerlessApplicationURL.DataContext = dataContext;
        }

        public override string Title => string.Format("Stack: {0}", this._controller.StackName);

        public override string UniqueId => string.Format("CloudFormation-Stack-{0}", this._controller.StackName);

        public Visibility VSToolkitDeployedFieldsVisibility
        {
            get
            {
                if (!this.IsEnabled)
                    return Visibility.Collapsed;

                return this._controller.Model.VSToolkitDeployedFieldsVisibility;
            }
        }

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            this._controller.Model.PropertyChanged += onPropertyChanged;
            checkIfTerminateAndDisable();
            setupTimerForNextCallback();

            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
            {
                BuildParameters();
                BuildOutputs();
            }));
            return this._controller.Model;
        }

        // The view is being redisplayed probably from a template deployment.  In that case we should do 
        // an immediate redeployment.
        public override void RefreshInitialData(object initialData)
        {
            this.onRefreshClick(this, new RoutedEventArgs());
        }

        void onPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs evnt)
        {
            if (string.Equals(evnt.PropertyName, "TemplateWrapper"))
            {
                try
                {
                    BuildParameters();
                    BuildOutputs();
                }
                catch (Exception e)
                {
                    LOGGER.Debug("Error handling property change", e);
                }
            }
            else if (string.Equals(evnt.PropertyName, "Status"))
            {
                this._ctlCancelStack.IsEnabled = string.Equals(CloudFormationConstants.UpdateInProgressStatus, this._controller.Model.Status);
            }
        }

        private void onConnectToInstance(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this._controller.Model.Instances.Count == 0)
                    this._ctlStackResources.LoadResources(false);
                this._controller.ConnectToInstance();
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to connect to instance", ex);
            }
        }

        private void onDeleteStack(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Stack?", "Are you sure you want to delete this stack?\r\n\r\nDeleting a stack will lead to deallocation of all the stack's resources"))
                {
                    this._controller.DeleteStack();
                    this.disableActions();
                    this.onRefreshClick(this, new RoutedEventArgs());
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to delete stack", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Failed to delete stack: " + ex.Message);
            }
        }

        private void onCancelUpdate(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ToolkitFactory.Instance.ShellProvider.Confirm("Cancel Update?", "Are you sure you want to cancel the update?\r\n\r\nCanceling the update will cause the stack to rollback to the previous stack configuration."))
                {
                    this._controller.CancelUpdate();
                    this.onRefreshClick(this, new RoutedEventArgs());
                    this._ctlCancelStack.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to delete stack", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Failed to delete stack: " + ex.Message);
            }
        }


        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {            
            if (Monitor.TryEnter(syncRoot, LOCK_WAIT))
            {
                refreshed = false;
                try
                {
                    this._controller.RefreshAll();

                    if (this._ctlMonitorTab.IsSelected)
                        this._ctlStackMonitoring.LoadCloudWatchData();
                    else
                        this._isMetricsLoaded = false;


                    if (this._ctlResourcesTab.IsSelected)
                        this._ctlStackResources.LoadResources(true);
                    else
                        this._isResourcesLoaded = false;

                    this.checkIfTerminateAndDisable();
                }
                catch (Exception ex)
                {
                    LOGGER.Error("Error refreshing stack", ex);
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing stack: " + ex.Message);
                }
                finally
                {
                    Monitor.Exit(syncRoot);
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

        private void onTabSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (this._ctlResourcesTab.IsSelected && !this._isResourcesLoaded)
            {
                this._isResourcesLoaded = true;
                this._ctlStackResources.LoadResources(true);
            }
            if (this._ctlMonitorTab.IsSelected && !this._isMetricsLoaded)
            {
                this._isMetricsLoaded = true;
                this._ctlStackMonitoring.LoadCloudWatchData();
            }
        }

        void onTimer(object source, ElapsedEventArgs evnt)
        {
            refreshed = false;

            if(Monitor.TryEnter(syncRoot, LOCK_WAIT))
            {
                try
                {
                    if (!refreshed)
                    {
                        //if(!ToolkitFactory.Instance.ShellProvider.IsEditorVisible(this))
                        //    return;
                        if (!this.IsVisible)
                        {
                            this._restartTimerOnNextVisibility = true;
                            return;
                        }

                        try
                        {
                            this._controller.Poll();

                            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                                {
                                    if (this._ctlTemplateOutputPanel.Children.Count != this._controller.Model.Outputs.Count)
                                        BuildOutputs();
                                }));

                            checkIfTerminateAndDisable();
                        }
                        catch (Exception e)
                        {                            
                            if(this._controller.Model.Status == CloudFormationConstants.DeleteInProgressStatus)
                            {
                                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                                {
                                    this._controller.Model.Status = CloudFormationConstants.DeleteCompleteStatus;
                                    checkIfTerminateAndDisable();
                                }));
                            }
                            LOGGER.Debug("Error during polling", e);
                        }
                        finally
                        {
                            System.Threading.Thread.MemoryBarrier();
                            this.setupTimerForNextCallback();
                            refreshed = true;
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(syncRoot);
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
            if (this._controller.Model.Status == CloudFormationConstants.DeleteCompleteStatus ||
                this._controller.Model.Status == CloudFormationConstants.RollbackCompleteStatus)
            {
                return;
            }

			if(this._pollingTimer != null)
			{
				if (this._controller.Model.Status == CloudFormationConstants.CreateInProgressStatus ||
					this._controller.Model.Status == CloudFormationConstants.DeleteInProgressStatus ||
                    this._controller.Model.Status == CloudFormationConstants.UpdateInProgressStatus ||
                    this._controller.Model.Status == CloudFormationConstants.UpdateCompleteCleanupInProgressStatus ||
                    this._controller.Model.Status == CloudFormationConstants.UpdateRollbackInProgressStatus ||
                    this._controller.Model.Status == CloudFormationConstants.UpdateRollbackCompleteCleanupInProgressStatus ||
					this._controller.Model.Status == CloudFormationConstants.RollbackInProgressStatus)			
					this._pollingTimer.Interval = NON_READY_POLLING;
				else
					this._pollingTimer.Interval = READY_POLLING;

				this._pollingTimer.Enabled = true;
			}
        }

        void checkIfTerminateAndDisable()
        {
            if (this._controller.Model.Status == CloudFormationConstants.DeleteCompleteStatus || this._controller.Model.Status == CloudFormationConstants.DeleteInProgressStatus)
            {
                disableActions();
            }
        }

        void disableActions()
        {
            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
            {
                this._ctlConnect.IsEnabled = false;
                this._ctlDeleteStack.IsEnabled = false;
            }));
        }

        public void BuildParameters()
        {
            this._ctlTemplateParameterPanel.Children.Clear();

            if (this._controller.Model != null && this._controller.Model != null && this._controller.Model.TemplateParameters != null)
            {
                foreach (var param in this._controller.Model.TemplateParameters.OrderBy(x => x.Name))
                {
                    if (!param.Hidden && !param.NoEcho)
                    {
                        var parameterUI = new TemplateParameterControl(param, true);
                        parameterUI.Width = double.NaN;
                        this._ctlTemplateParameterPanel.Children.Add(parameterUI);
                    }
                }
            }
        }

        public void BuildOutputs()
        {
            this._ctlTemplateOutputPanel.Children.Clear();

            if (this._controller.Model != null && this._controller.Model != null && this._controller.Model.Outputs != null)
            {
                foreach (var output in this._controller.Model.Outputs.OrderBy(x => x.OutputKey))
                {
                    var outputUI = new TemplateOutputControl(output);
                    outputUI.Width = double.NaN;
                    this._ctlTemplateOutputPanel.Children.Add(outputUI);
                }
            }
        }

        private void onApplicationURLRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.ToString()));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating to object: " + ex.Message);
            }
        }

        private void onCopyClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                System.Windows.Clipboard.SetText(this._ctlServerlessLinkInner.Text);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error copying Serverless URL URL", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error copying Serverless URL: " + e.Message);
            }
        }
    }
}
