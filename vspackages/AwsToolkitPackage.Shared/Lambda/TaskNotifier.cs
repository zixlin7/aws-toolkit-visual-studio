using System;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.Lambda
{
   public class TaskNotifier
    {
        /// <summary>
        /// Notifies the progress of a background task in the Visual Studio Task Status Center service <see cref="IVsTaskStatusCenterService"/>
        /// </summary>
        /// <param name="taskCreator">background task whose status is indicated</param>
        public async Task ShowTaskStatusAsync(Func<Task> taskCreator, AWSToolkitPackage hostPackage)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var taskStatusCenter = await hostPackage.GetServiceAsync(typeof(SVsTaskStatusCenterService)) as IVsTaskStatusCenterService;
            Assumes.Present(taskStatusCenter);

            var options = default(TaskHandlerOptions);
            options.Title = "Installing Mock Lambda Test Tool";
            options.ActionsAfterCompletion = CompletionActions.None;

            var data = default(TaskProgressData);
            data.CanBeCanceled = false;
            data.ProgressText = "Loading...";

            var handler = taskStatusCenter.PreRegister(options, data);
            handler.RegisterTask(taskCreator());

            handler.Progress.Report(data);
        }
    }
}
