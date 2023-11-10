using System;
using System.Collections.Generic;

namespace Amazon.AWSToolkit.VisualStudio.SupportedVersion
{
    public class SunsetNotificationSettings : IEquatable<SunsetNotificationSettings>
    {
        public Dictionary<string, int> DisplayedNotifications { get; set; } = new Dictionary<string, int>();

        public int GetDisplayedNotificationVersion(string sunsetNotificationId, int defaultNotificationVersion)
        {
            if (DisplayedNotifications == null || !DisplayedNotifications.TryGetValue(sunsetNotificationId, out var notificationVersion))
            {
                notificationVersion = defaultNotificationVersion;
            }

            return notificationVersion;
        }

        public void SetDisplayedNotificationVersion(string sunsetNotificationId, int notificationVersion)
        {
            if (DisplayedNotifications == null)
            {
                DisplayedNotifications = new Dictionary<string, int>();
            }

            DisplayedNotifications[sunsetNotificationId] = notificationVersion;
        }

        public bool Equals(SunsetNotificationSettings other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(DisplayedNotifications, other.DisplayedNotifications);
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

            return Equals((SunsetNotificationSettings)obj);
        }

        public override int GetHashCode()
        {
            return (DisplayedNotifications != null ? DisplayedNotifications.GetHashCode() : 0);
        }
    }
}
