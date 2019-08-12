using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Toaster panel to inform user that their application deployment has 
    /// completed and is ready for use.
    /// </summary>
    public partial class DeploymentNotificationPanel
    {
        public DeploymentNotificationPanel()
        {
            InitializeComponent();
        }

        public string EndPointURL { get; set; }

        public void SetPanelContent(string applicationName, string environmentName, string endpointURL, bool success)
        {
            if (success)
            {
                string msg = string.Format("Environment '{0}' has completed deployment for application '{1}'.", environmentName, applicationName);
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
                string msg = string.Format("Environment '{0}' has failed to deploy for application '{1}'.", environmentName, applicationName);
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
