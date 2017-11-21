using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.Model;
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

        public override string Title
        {
            get
            {
                return string.Format("ECS Cluster: {0}", this._controller.FeatureViewModel.Name);
            }
        }

        public override string UniqueId
        {
            get
            {
                return "Cluster: " + this._controller.EndPoint + "_" + this._controller.Account.SettingsUniqueKey + "_" + this._controller.FeatureViewModel.Name;
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
