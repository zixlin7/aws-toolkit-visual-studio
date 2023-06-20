using System.Collections.Generic;

using Amazon.AWSToolkit.Settings;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Settings
{
    public class NotificationSettingsTest
    {
        private static readonly NotificationSettings _emptySettings = new NotificationSettings();
        private static readonly NotificationSettings _singleNotificationSettings = new NotificationSettings()
        {
            DismissedNotifications = new List<NotificationSettings.DismissedNotification>
            {
                new NotificationSettings.DismissedNotification() {NotificationId = "01234", DismissedOn = 1680332400}
            }
        };
        private static readonly NotificationSettings _multipleNotificationSettings = new NotificationSettings()
        {
            DismissedNotifications = new List<NotificationSettings.DismissedNotification>
            {
                new NotificationSettings.DismissedNotification() {NotificationId = "01234", DismissedOn = 1680332400},
                new NotificationSettings.DismissedNotification() {NotificationId = "56789", DismissedOn = 1680332400}
            }
        };
        private static readonly NotificationSettings _reorderedMultipleNotificationSettings = new NotificationSettings()
        {
            DismissedNotifications = new List<NotificationSettings.DismissedNotification>
            {
                new NotificationSettings.DismissedNotification() {NotificationId = "56789", DismissedOn = 1680332400},
                new NotificationSettings.DismissedNotification() {NotificationId = "01234", DismissedOn = 1680332400},
            }
        };

        [Theory]
        [MemberData(nameof(GetNotificationData))]
        public void CompareLists(NotificationSettings settings1, NotificationSettings settings2, bool expected)
        {
            Assert.Equal(expected, settings1.Equals(settings2));
        }

        public static TheoryData<NotificationSettings, NotificationSettings, bool> GetNotificationData()
        {
            return new TheoryData<NotificationSettings, NotificationSettings, bool>()
            {
                { _emptySettings, _emptySettings, true },
                { _emptySettings, _singleNotificationSettings, false },
                { _singleNotificationSettings, _singleNotificationSettings, true },
                { _singleNotificationSettings, _multipleNotificationSettings, false },
                { _multipleNotificationSettings, _singleNotificationSettings, false },
                { _multipleNotificationSettings, _multipleNotificationSettings, true },
                { _multipleNotificationSettings, _reorderedMultipleNotificationSettings, true }
            };
        }
    }
}
