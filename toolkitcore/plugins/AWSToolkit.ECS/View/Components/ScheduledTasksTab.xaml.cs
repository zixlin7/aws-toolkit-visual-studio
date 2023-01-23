using System;
using System.Collections.Generic;
using System.Windows;

using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.Navigator;

using log4net;

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
                this._controller.ToolkitContext.ToolkitHost.ShowError("Serivces Load Error", msg);
            }
        }

        private void onRefreshClick(object sender, RoutedEventArgs e)
        {
            this.RefreshScheduledTasks();
        }

        private void Delete_Click(object sender, RoutedEventArgs evnt)
        {
            var results = TryDeleteScheduledTask(out var count);
            _controller.RecordDeleteScheduledTask(count, results);
        }

        private ActionResults TryDeleteScheduledTask(out int taskCount)
        {
            taskCount = 0;
            if (!_controller.ToolkitContext.ToolkitHost.Confirm("Delete Scheduled Task", "Are you want to delete the selected scheduled tasks?"))
            {
                return ActionResults.CreateCancelled();
            }

            try
            {
                var tasks = new List<ScheduledTaskWrapper>();
                foreach (ScheduledTaskWrapper task in _ctlTasks.SelectedItems)
                {
                    tasks.Add(task);
                }
                taskCount = tasks.Count;
                _controller.DeleteScheduleTasks(tasks);
                _controller.RefreshScheduledTasks();
                return new ActionResults().WithSuccess(true);
            }
            catch (Exception e)
            {
                var msg = "Error stopping tasks for cluster: " + e.Message;
                LOGGER.Error(msg, e);
                _controller.ToolkitContext.ToolkitHost.ShowError("Serivces Load Error", msg);
                return ActionResults.CreateFailed(e);
            }
        }

        void onTasksSelectionChanged(object sender, RoutedEventArgs evnt)
        {
            this._ctlDelete.IsEnabled = this._ctlTasks.SelectedItems.Count > 0;
        }
    }
}
