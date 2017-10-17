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

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ECS.Controller;

using log4net;
using System.Diagnostics;

namespace Amazon.AWSToolkit.ECS.View.Components
{
    /// <summary>
    /// Interaction logic for ServicesTab.xaml
    /// </summary>
    public partial class ServicesTab
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(ServicesTab));

        ViewClusterController _controller;

        public ServicesTab()
        {
            InitializeComponent();
        }

        public void Initialize(ViewClusterController controller)
        {
            this._controller = controller;
        }

        public void RefreshServices()
        {
            try
            {
                this._controller.RefreshServices();
            }
            catch (Exception e)
            {
                var msg = "Error fetching services for cluster";
                LOGGER.Error(msg, e);
                ToolkitFactory.Instance.ShellProvider.ShowError(msg, "Serivces Load Error");
            }
        }

        private void onServiceURLClick(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                var uri = e.Uri.ToString();
                if (uri.EndsWith("*"))
                    uri = uri.Substring(0, uri.Length - 1);
                Process.Start(new ProcessStartInfo(uri));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating to load balancer: " + ex.Message);
            }
        }

        private void onHealthCheckURLClick(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.ToString()));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating to health check: " + ex.Message);
            }
        }
    }
}
