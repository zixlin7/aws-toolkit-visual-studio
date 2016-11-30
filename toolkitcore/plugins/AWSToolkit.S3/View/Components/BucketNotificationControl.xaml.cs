using System;
using System.Collections.Generic;
using System.ComponentModel;
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

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Controller;

using log4net;

namespace Amazon.AWSToolkit.S3.View.Components
{
    /// <summary>
    /// Interaction logic for BucketNotificationControl.xaml
    /// </summary>
    public partial class BucketNotificationControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(BucketNotificationControl));


        public BucketNotificationControl()
        {
            InitializeComponent();
        }

        BucketPropertiesController _controller;
        public void Initialize(BucketPropertiesController controller)
        {
            this._controller = controller;
        }

        private void EventConfigurationDelete_OnClick(object sender, RoutedEventArgs evnt)
        {
            evnt.Handled = true;

            var btn = evnt.Source as Button;
            if (btn == null)
                return;

            var wrapper = btn.DataContext as EventConfigurationModel;
            if (wrapper == null)
                return;

            var message = string.Format("Are you sure you want to delete the event configuration to {0}?", wrapper.FormattedResourceName);
            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Event Configuration", message))
                return;

            try
            {
                this._controller.DeleteEventConfiguration(wrapper);
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to delete event configuration", e);
                ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("Error deleting event configuration: {0}", e.Message));
            }
        }

        private void AddEventConfiguration_Click(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.AddEventConfiguration();
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to add event configuration", e);
                ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("Error adding event configuration: {0}", e.Message));
            }
        }
    }
}
