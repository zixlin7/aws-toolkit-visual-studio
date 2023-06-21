using System.Threading.Tasks;

namespace Amazon.AWSToolkit.VisualStudio.Notification
{
    public class DontShowAgainAction : INotificationAction
    {
        public bool DisplayAgain { get; set; } = false;
        public bool Dismiss { get; set; } = true;

        public async Task InvokeAsync(NotificationStrategy strategy)
        {
        }
    }
}
