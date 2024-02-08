using Amazon.AWSToolkit.Exceptions;

namespace Amazon.AWSToolkit.CommonUI.Notifications
{
    public static class TaskResultExtensionMethods
    {
        public static void ThrowIfUnsuccessful(this TaskResult result)
        {
            switch (result.Status)
            {
                case TaskStatus.Success:
                    return;
                case TaskStatus.Cancel:
                    throw new UserCanceledException("User canceled login flow");
                default:
                    throw result.Exception;
            }
        }
    }
}
