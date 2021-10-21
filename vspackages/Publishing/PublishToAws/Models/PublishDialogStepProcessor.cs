using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.Notifications.Progress;

using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.Publish.Models
{
    public abstract class ProgressStepProcessor
    {
        public abstract Task ProcessStepsAsync(IProgressDialog progressDialog, IList<ShowPublishDialogStep> steps);
    }

    public class PublishDialogStepProcessor : ProgressStepProcessor
    {
        /// <summary>
        /// Invokes the actions provided in <see cref="steps"/>, and updates the dialog along the way.
        /// 
        /// Raises OperationCanceledException if the user cancels the Progress dialog.
        /// </summary>
        /// <param name="progressDialog">The progress dialog to update while performing actions</param>
        /// <param name="steps">Actions to perform while updating the dialog</param>
        public override async Task ProcessStepsAsync(IProgressDialog progressDialog, IList<ShowPublishDialogStep> steps)
        {
            foreach (var step in steps)
            {
                progressDialog.CanCancel = step.CanCancel;
                progressDialog.CurrentStep++;
                progressDialog.Heading1 = step.Description;

                // Cancelling does not stop the step's task, but allows the dialog to close
                await step.StartTaskAsync().WithCancellation(progressDialog.CancellationToken).ConfigureAwait(false);

                if (progressDialog.IsCancelRequested() || progressDialog.CancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
            }
        }
    }
}
