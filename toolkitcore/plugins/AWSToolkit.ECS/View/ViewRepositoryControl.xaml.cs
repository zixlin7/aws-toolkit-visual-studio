using System;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for ViewRepositoryControl.xaml
    /// </summary>
    public partial class ViewRepositoryControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewRepositoryControl));

        readonly ViewRepositoryController _controller;

        public ViewRepositoryControl(ViewRepositoryController controller)
        {
            this._controller = controller;
            InitializeComponent();
            this._ctlPushCommands.Initialize(controller);
        }

        public override string Title => string.Format("Repository {0}", this._controller.RepositoryName);

        public override string UniqueId =>
            string.Format("Repository: {0} {1}_{2}",
                this._controller.RepositoryArn,
                this._controller.Region.Id,
                this._controller.CredentialIdentifier.Id);

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }
        
        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordEcsOpenRepository(new EcsOpenRepository()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }
        
        void onLoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
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
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing repository data: " + e.Message);
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
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing repository data: " + e.Message);
            }
        }

        void SortHandler(object sender, DataGridSortingEventArgs e)
        {
            
        }

        private void ViewPushCommands_OnClick(object sender, RoutedEventArgs e)
        {
            if (_ctlPushCommandsFlyover.Visibility == Visibility.Collapsed)
            {
                _ctlPushCommandsFlyover.Visibility = Visibility.Visible;
                _ctlViewHidePushCommands.Content = "Hide Push Commands";
            }
            else
            {
                _ctlPushCommandsFlyover.Visibility = Visibility.Collapsed;
                _ctlViewHidePushCommands.Content = "View Push Commands";
            }
        }

    }
}
