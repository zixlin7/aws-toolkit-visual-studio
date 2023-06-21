using System;

namespace Amazon.AWSToolkit.Exceptions
{
    public class NotificationToolkitException : ToolkitException
    {
        public enum NotificationErrorCode
        {
            UnsupportedIdeVersion,
            InvalidFetchVersionRequest,
            InvalidVersionManifest,
            InvalidFetchNotificationRequest,
            InvalidNotification,
            InvalidCacheCleanup,
            InvalidToolkitVersion,
            InvalidComparator,
            InvalidNotificationDismissal,
            InvalidDismissedNotificationLookup,
            UnsupportedGesture,
            UnsupportedActionContext,
        }

        public NotificationToolkitException(string message, NotificationErrorCode errorCode) : this(message, errorCode, null) {}

        public NotificationToolkitException(string message, NotificationErrorCode errorCode, Exception e)
            : base(message, errorCode.ToString(), e) { }
    }
}
