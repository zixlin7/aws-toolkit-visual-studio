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

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Model;

using log4net;


namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    /// Interaction logic for EventSourcesControl.xaml
    /// </summary>
    public partial class EventSourcesControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(EventSourcesControl));

        ViewFunctionController _controller;

        public EventSourcesControl()
        {
            InitializeComponent();
        }

        public void Initialize(ViewFunctionController controller)
        {
            this._controller = controller;
        }

        private void EventSourceDelete_OnClick(object sender, RoutedEventArgs evnt)
        {
            evnt.Handled = true;

            var btn = evnt.Source as Button;
            if (btn == null)
                return;

            var wrapper = btn.DataContext as EventSourceWrapper;
            if (wrapper == null)
                return;

            var message = string.Format("Are you sure you want to delete the event source to {0}?", wrapper.ResourceDisplayName);
            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Event Source", message))
                return;

            try
            {
                this._controller.DeleteEventSource(wrapper);
                this._controller.RefreshEventSources();
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to delete event source", e);
                ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("Error deleting event source: {0}", e.Message));
            }
        }

        private void AddEventSource_Click(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._controller.AddEventSource())
                {
                    this._controller.RefreshEventSources();
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to add event source", e);
                ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("Error adding event source: {0}", e.Message));
            }
        }
    }
}
