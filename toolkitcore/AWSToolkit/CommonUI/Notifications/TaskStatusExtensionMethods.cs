using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.CommonUI.Notifications
{
    public static class TaskStatusExtensionMethods
    {
        public static Result AsTelemetryResult(this TaskStatus taskStatus)
        {
            switch (taskStatus)
            {
                case TaskStatus.Success:
                    return Result.Succeeded;
                case TaskStatus.Cancel:
                    return Result.Cancelled;
                case TaskStatus.Fail:
                    return Result.Failed;
                default:
                    return Result.Failed;
            }
        }
    }
}
