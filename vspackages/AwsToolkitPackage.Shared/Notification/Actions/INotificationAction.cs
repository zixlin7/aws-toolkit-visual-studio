using System.Threading.Tasks;

namespace Amazon.AWSToolkit.VisualStudio.Notification
{
    public interface INotificationAction
    {
        bool DisplayAgain { get; set; }
        bool Dismiss { get; set; }

        Task InvokeAsync(NotificationStrategy strategy);
    }
}
