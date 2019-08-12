using System;
using Amazon.AWSToolkit.ECS.Controller;

using log4net;

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
