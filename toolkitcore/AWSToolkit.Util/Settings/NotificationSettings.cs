using System;
using System.Collections.Generic;
using System.Linq;

namespace Amazon.AWSToolkit.Settings
{
    /// <summary>
    /// Represents all notification related toolkit settings
    /// </summary>
    public class NotificationSettings : IEquatable<NotificationSettings>
    {
        public List<DismissedNotification> DismissedNotifications { get; set; }

        public class DismissedNotification : IEquatable<DismissedNotification>
        {
            public string NotificationId { get; set; }
            public long DismissedOn { get; set; }

            public bool Equals(DismissedNotification other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return NotificationId.Equals(other.NotificationId) && DismissedOn.Equals(other.DismissedOn);
            }
        }

        public bool Equals(NotificationSettings other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (DismissedNotifications == null && other.DismissedNotifications == null)
            {
                return true;
            }

            if (DismissedNotifications == null || other.DismissedNotifications == null)
            {
                return false;
            }

            return DismissedNotifications.All(other.DismissedNotifications.Contains) && DismissedNotifications.Count == other.DismissedNotifications.Count;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((NotificationSettings)obj);
        }
    }
}
