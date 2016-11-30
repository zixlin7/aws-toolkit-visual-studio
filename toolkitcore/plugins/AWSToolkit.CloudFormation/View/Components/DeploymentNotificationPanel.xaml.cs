using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

using Amazon.AWSToolkit.CommonUI;
using log4net;

namespace Amazon.AWSToolkit.CloudFormation.View.Components
{
    /// <summary>
    /// Interaction logic for DeploymentNotificationPanel.xaml
    /// </summary>
    public partial class DeploymentNotificationPanel
    {
        public DeploymentNotificationPanel()
        {
            InitializeComponent();
        }

        public string EndPointURL { get; set; }

        public void SetPanelContent(string applicationName, string endpointURL, bool success)
        {
            if (success)
            {
                string msg = string.Format("'{0}' has completed deployment.", applicationName);
                this._message.Text = msg;
                this._message.ToolTip = msg;
                if (!string.IsNullOrEmpty(endpointURL))
                    EndPointURL = endpointURL;
                else
                    _link.Visibility = Visibility.Hidden;
                _successImage.Visibility = Visibility.Visible;
            }
            else
            {
                string msg = string.Format("'{0}' has failed to deploy.", applicationName);
                this._message.Text = msg;
                this._message.ToolTip = msg;
                EndPointURL = string.Empty;
                _link.Visibility = Visibility.Hidden;
                _failImage.Visibility = Visibility.Visible;
            }
        }

        // using mouse button handler because Hyperlink NavigateRequest wire-up would not trigger
        private void _link_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(EndPointURL))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(EndPointURL));
                }
                catch (Exception ex)
                {
                    ILog LOGGER = LogManager.GetLogger(typeof(DeploymentNotificationPanel));
                    LOGGER.Error("Failed to launch process to go to endpoint", ex);
                }
            }
        }

    }
}
