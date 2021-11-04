using System;
using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    public interface IErrorListReporter
    {
        TaskProvider.TaskCollection Tasks {get;}

#if VS2022_OR_LATER
        bool Navigate(TaskListItem task, Guid logicalView);
#else
        bool Navigate(Microsoft.VisualStudio.Shell.Task task, Guid logicalView);
#endif
        void ResumeRefresh();
        void SuspendRefresh();
    }
}
