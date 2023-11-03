using System;

namespace Amazon.AWSToolkit.CommonUI.Notifications
{
    public class TaskResult
    {
        public TaskStatus Status { get; set; } = TaskStatus.Fail;

        public Exception Exception { get; set; }
    }
}
