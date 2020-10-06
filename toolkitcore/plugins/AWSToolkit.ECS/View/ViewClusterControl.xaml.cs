using System;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for ViewClusterControl.xaml
    /// </summary>
    public partial class ViewClusterControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewClusterControl));

        readonly ViewClusterController _controller;

        public ViewClusterControl(ViewClusterController controller)
        {
            InitializeComponent();
            this._controller = controller;

            this._ctlServices.Initialize(this._controller);
            this._ctlTasks.Initialize(this._controller);
            this._ctlScheduledTasks.Initialize(this._controller);

        }

        public override string Title => string.Format("ECS Cluster: {0}", this._controller.FeatureViewModel.Name);

        public override string UniqueId => "Cluster: " + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.SettingsUniqueKey + "_" + this._controller.FeatureViewModel.Name;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordEcsOpenCluster(new EcsOpenCluster()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }
        
        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Refresh();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing cluster: " + e.Message);
            }
        }

        public override void RefreshInitialData(object initialData)
        {
            try
            {
                this._controller.Refresh();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing cluster: " + e.Message);
            }
        }

        public bool SaveService(ServiceWrapper service)
        {
            try
            {
                return this._controller.SaveService(service);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving service: " + e.Message);
                return false;
            }
        }

        public void DeleteService(ServiceWrapper service)
        {
            try
            {
                this._controller.DeleteService(service);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting service: " + e.Message);
            }
        }

        private void onTabSelectionChange(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
