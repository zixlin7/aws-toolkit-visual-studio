using System;
using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    public interface IErrorListReporter
    {
        TaskProvider.TaskCollection Tasks {get;}

        bool Navigate(Microsoft.VisualStudio.Shell.Task task, Guid logicalView);

        void ResumeRefresh();
        void SuspendRefresh();
    }
}
