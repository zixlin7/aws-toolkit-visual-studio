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
