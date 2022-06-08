using System;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.CommonUI.Notifications
{
    /// <summary>
    /// Notifies the progress of a background task in the Visual Studio Task Status Center Service <see cref="IVsTaskStatusCenterService"/>
    /// </summary>
    public interface ITaskStatusNotifier
    {
        /// <summary>
        /// Represents if a user has pressed the Cancel button on the dialog
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// The task's title
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// The task's progress text
        /// </summary>
        string ProgressText { get; set; }

        /// <summary>
        /// Whether or not users are allowed to cancel the task while processing is taking place
        /// </summary>
        bool CanCancel { get; set; }

        /// <summary>
        /// Indicates the provided task's status in Task Status Center
        /// </summary>
        /// <param name="taskCreator">background task whose status is indicated</param>
        void ShowTaskStatus(Func<ITaskStatusNotifier, Task> taskCreator);
    }
}
