using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.Notifications;

using Microsoft.VisualStudio.TaskStatusCenter;

namespace AwsToolkit.VsSdk.Common.Notifications
{
    public class TaskStatusNotifier : ITaskStatusNotifier
    {
        public const string DefaultProgressText = "Loading..";
        private string _progressText = DefaultProgressText;
        private bool _isTaskRegistered = false;

        private readonly IVsTaskStatusCenterService _taskStatusCenter;
        private ITaskHandler _handler;

        public TaskStatusNotifier(IVsTaskStatusCenterService taskStatusCenter)
        {
            _taskStatusCenter = taskStatusCenter;
        }

        public CancellationToken CancellationToken => _handler.UserCancellation;

        public string Title { get; set; }

        public bool CanCancel { get; set; } = false;

        public string ProgressText
        {
            get => _progressText;
            set
            {
                _progressText = value;
                Update();
            }
        }

        /// <summary>
        /// Updates progress reported 
        /// </summary>
        private void Update()
        {
            if (_isTaskRegistered)
            {
                var data = new TaskProgressData
                {
                    ProgressText = ProgressText, CanBeCanceled = CanCancel, PercentComplete = null,
                };

                _handler?.Progress.Report(data);
            }
        }

        /// <summary>
        /// Indicates the provided task's status in Task Status Center
        /// </summary>
        /// <param name="taskCreator">background task whose status is indicated</param>
        public void ShowTaskStatus(Func<ITaskStatusNotifier,Task> taskCreator)
        {
            _handler = SetupTaskHandler();
            _handler.RegisterTask(taskCreator(this));

            _isTaskRegistered = true;

            Update();
        }

        /// <summary>
        /// Sets up task handler using properties specified
        /// </summary>
        /// <returns></returns>
        private ITaskHandler SetupTaskHandler()
        {
            var options = default(TaskHandlerOptions);
            options.Title = Title;
            options.ActionsAfterCompletion = CompletionActions.None;

            var data = default(TaskProgressData);
            data.CanBeCanceled = CanCancel;
            data.ProgressText = DefaultProgressText;

            return _taskStatusCenter.PreRegister(options, data);
        }
    }
}
