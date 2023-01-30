using System;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Represents a unit of work in starting up the Publish to AWS experience,
    /// along with its progress related descriptive information.
    /// </summary>
    public class ShowPublishDialogStep
    {
        private readonly Func<CancellationToken, Task> _taskCreator;

        /// <summary>
        /// The name of the step that would be shown in a progress indicator
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Whether or not the user is allowed to cancel during this step
        /// </summary>
        public bool CanCancel { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="taskCreator">Function that contains the actions to be performed by this step</param>
        /// <param name="description">Shown in a progress indicator while this step is performed</param>
        /// <param name="canCancel">Whether or not users can cancel during this step</param>
        public ShowPublishDialogStep(Func<CancellationToken, Task> taskCreator, string description, bool canCancel)
        {
            _taskCreator = taskCreator;
            Description = description;
            CanCancel = canCancel;
        }

        /// <summary>
        /// Called to start performing this step's actions
        /// </summary>
        public Task StartTaskAsync(CancellationToken cancellationToken)
        {
            return _taskCreator(cancellationToken);
        }
    }
}
