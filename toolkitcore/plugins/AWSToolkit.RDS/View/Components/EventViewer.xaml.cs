using System;
using System.Windows;
using Amazon.AWSToolkit.RDS.Controller;

namespace Amazon.AWSToolkit.RDS.View.Components
{
    /// <summary>
    /// Interaction logic for EventViewer.xaml
    /// </summary>
    public partial class EventViewer
    {
        IEventController _controller;
        public EventViewer()
        {
            InitializeComponent();
        }

        public void Initialize(IEventController controller)
        {
            this._controller = controller;
        }

        void onRefreshEvents(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshSelectedEvents();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error loading events: " + e.Message);
            }
        }
    }
}
