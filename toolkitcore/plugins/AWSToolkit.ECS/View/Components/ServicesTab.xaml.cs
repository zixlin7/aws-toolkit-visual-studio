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
                var msg = "Error fetching services for cluster: " + e.Message;
                LOGGER.Error(msg, e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Serivces Load Error", msg);
            }
        }
    }
}
