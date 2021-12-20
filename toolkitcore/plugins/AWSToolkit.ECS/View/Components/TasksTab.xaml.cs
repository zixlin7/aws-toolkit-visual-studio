using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Amazon.ECS;
using Amazon.AWSToolkit.ECS.Controller;

using log4net;
using Amazon.AWSToolkit.ECS.Model;

namespace Amazon.AWSToolkit.ECS.View.Components
{
    /// <summary>
    /// Interaction logic for TasksTab.xaml
    /// </summary>
    public partial class TasksTab
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(TasksTab));

        ViewClusterController _controller;

        public TasksTab()
        {
            InitializeComponent();
        }

        public void Initialize(ViewClusterController controller)
        {
            this._controller = controller;
        }

        public void RefreshTasks()
        {
            try
            {
                this._controller.RefreshTasks();
            }
            catch (Exception e)
            {
                var msg = "Error fetching tasks for cluster: " + e.Message;
                LOGGER.Error(msg, e);
                this._controller.ToolkitContext.ToolkitHost.ShowError("Serivces Load Error", msg);
            }
        }

        private void onRefreshClick(object sender, RoutedEventArgs e)
        {
            this.RefreshTasks();
        }

        private void Stop_Click(object sender, RoutedEventArgs evnt)
        {
            if (!this._controller.ToolkitContext.ToolkitHost.Confirm("Stop Task(s)", "Are you want to stop the selected task(s)?"))
                return;

            try
            {
                var tasks = new List<TaskWrapper>();
                foreach (TaskWrapper task in _ctlListOfTasks.SelectedItems)
                {
                    tasks.Add(task);
                }
                this._controller.StopTasks(tasks);
                this._controller.RefreshTasks();
            }
            catch (Exception e)
            {
                var msg = "Error stopping tasks for cluster: " + e.Message;
                LOGGER.Error(msg, e);
                this._controller.ToolkitContext.ToolkitHost.ShowError("Serivces Load Error", msg);
            }
        }

        private void StopAll_Click(object sender, RoutedEventArgs evnt)
        {
            if (!this._controller.ToolkitContext.ToolkitHost.Confirm("Stop All Tasks", "Are you want to stop all tasks?"))
                return;

            try
            {
                this._controller.StopTasks(this._controller.Model.Tasks);
                this._controller.RefreshTasks();
            }
            catch (Exception e)
            {
                var msg = "Error stopping tasks for cluster: " + e.Message;
                LOGGER.Error(msg, e);
                this._controller.ToolkitContext.ToolkitHost.ShowError("Serivces Load Error", msg);
            }
        }

        

        private void DesiredStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this._controller == null || this._controller.Model == null)
                return;

            if (this._ctlDesiredStatus.SelectedIndex == 0)
            {
                this._controller.Model.TaskTabDesiredStatus = DesiredStatus.RUNNING;
                this._ctlStopAll.IsEnabled = true;
            }
            else if (this._ctlDesiredStatus.SelectedIndex == 1)
            {
                this._controller.Model.TaskTabDesiredStatus = DesiredStatus.STOPPED;
                this._ctlStopAll.IsEnabled = false;
            }

            RefreshTasks();
        }

        void onTasksSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this._ctlStop.IsEnabled = this._ctlListOfTasks.SelectedItems.Count > 0 && this._controller.Model.TaskTabDesiredStatus == DesiredStatus.RUNNING;
        }
    }
}
