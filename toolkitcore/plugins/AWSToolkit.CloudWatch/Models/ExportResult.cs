using System;

using Amazon.AWSToolkit.CommonUI.Notifications;

namespace Amazon.AWSToolkit.CloudWatch.Models
{
    public class ExportResult
    {
        public int Count;
        public TaskStatus Status;
        public Exception Exception;
    }
}
