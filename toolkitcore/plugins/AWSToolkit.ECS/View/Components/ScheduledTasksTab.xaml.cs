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
using Amazon.AWSToolkit.ECS.Model;

namespace Amazon.AWSToolkit.ECS.View.Components
{
    /// <summary>
    /// Interaction logic for ScheduledTasksTab.xaml
    /// </summary>
    public partial class ScheduledTasksTab
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(ScheduledTasksTab));

        ViewClusterController _controller;

        public ScheduledTasksTab()
        {
            InitializeComponent();
        }

        public void Initialize(ViewClusterController controller)
        {
            this._controller = controller;
        }

        public void RefreshScheduledTasks()
        {
            try
            {
                this._controller.RefreshScheduledTasks();
            }
            catch (Exception e)
            {
                var msg = "Error fetching scheduled tasks for cluster: " + e.Message;
                LOGGER.Error(msg, e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Serivces Load Error", msg);
            }
        }

        private void onRefreshClick(object sender, RoutedEventArgs e)
        {
            this.RefreshScheduledTasks();
        }

        private void Delete_Click(object sender, RoutedEventArgs evnt)
        {
            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Scheduled Task", "Are you want to delete the selected scheduled tasks?"))
                return;

            try
            {
                var tasks = new List<ScheduledTaskWrapper>();
                foreach (ScheduledTaskWrapper task in _ctlTasks.SelectedItems)
                {
                    tasks.Add(task);
                }
                this._controller.DeleteScheduleTasks(tasks);
                this._controller.RefreshScheduledTasks();
            }
            catch (Exception e)
            {
                var msg = "Error stopping tasks for cluster: " + e.Message;
                LOGGER.Error(msg, e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Serivces Load Error", msg);
            }
        }

        void onTasksSelectionChanged(object sender, RoutedEventArgs evnt)
        {
            this._ctlDelete.IsEnabled = this._ctlTasks.SelectedItems.Count > 0;
        }
    }
}
