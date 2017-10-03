using System;
using System.Windows;
using Amazon.AWSToolkit.ECS.Controller;
using log4net;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for ViewClustersControl.xaml
    /// </summary>
    public partial class ViewClustersControl 
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewClustersControl));

        readonly ViewClustersController _controller;

        public ViewClustersControl(ViewClustersController controller)
        {
            InitializeComponent();
            this._controller = controller;
        }

        public override string Title
        {
            get
            {
                return string.Format("{0} ECS Clusters", this._controller.RegionDisplayName);
            }
        }

        public override string UniqueId
        {
            get
            {
                return "Clusters: " + this._controller.EndPoint + "_" + this._controller.Account.SettingsUniqueKey;
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
                // todo this._controller.RefreshInstances();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing cluster: " + e.Message);
            }
        }

        void onLaunchClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                /* todo
                IList<RunningInstanceWrapper> newInstances = this._controller.LaunchInstance();
                if (newInstances != null && newInstances.Count > 0)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(newInstances[0]);
                }*/
            }
            catch (Exception e)
            {
                LOGGER.Error("Error launching cluster", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error launching ECS cluster: " + e.Message);
            }
        }

    }
}
