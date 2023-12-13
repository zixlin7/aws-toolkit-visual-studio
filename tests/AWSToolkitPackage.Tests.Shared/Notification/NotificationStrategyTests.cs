using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Settings;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.Settings;
using Amazon.AWSToolkit.VisualStudio.Notification;

using Xunit;

using Notif = Amazon.AWSToolkit.VisualStudio.Notification.Notification;

namespace AWSToolkitPackage.Tests.Notification
{
    public class NotificationStrategyTests
    {
        private readonly NotificationStrategy _sut;
        private readonly FakeSettingsRepository<NotificationSettings> _notificationRepository =
            new FakeSettingsRepository<NotificationSettings>();
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private static readonly Notif _sampleNotification = BuildNotification("uuid-1");
        private static readonly Notif _sampleNotification2 = BuildNotification("uuid-2");

        public NotificationStrategyTests()
        {
            _notificationRepository.Settings = new NotificationSettings() { DismissedNotifications = new List<NotificationSettings.DismissedNotification>() };
            _sut = new NotificationStrategy(_notificationRepository,
                _toolkitContextFixture.ToolkitContext, "1.39.0.0", Component.Infobar);
        }

        [Fact]
        public async Task ShouldMarkNotificationAsDismissed()
        {
            await _sut.MarkNotificationAsDismissedAsync(_sampleNotification);

            Assert.Equal(_sampleNotification.NotificationId, _notificationRepository.Settings.DismissedNotifications.FirstOrDefault()?.NotificationId);
        }
        
        [Theory]
        [MemberData(nameof(GetNotificationData))]
        public async Task ShouldCheckIfNotificationHasBeenDismissed(List<Notif> savedNotifications, Notif stagedNotification, bool expected)
        {
            var listOfTasks = new List<Task>();

            savedNotifications?.ForEach(notification => listOfTasks.Add(_sut.MarkNotificationAsDismissedAsync(notification)));

            if (listOfTasks.Any())
            {
                await Task.WhenAll(listOfTasks);
            }

            var hasUserDismissedNotification = await _sut.HasUserDismissedNotificationAsync(stagedNotification);

            Assert.Equal(expected, hasUserDismissedNotification);
        }

        public static TheoryData<List<Notif>, Notif, bool> GetNotificationData()
        {
            return new TheoryData<List<Notif>, Notif, bool>()
            {
                { new List<Notif>(), _sampleNotification, false },
                { new List<Notif>(){ _sampleNotification }, _sampleNotification, true },
                { new List<Notif>(){ _sampleNotification2 }, _sampleNotification, false },
                { new List<Notif>(){ _sampleNotification2, _sampleNotification }, _sampleNotification, true }
            };
        }

        private static Notif BuildNotification(string uuid)
        {
            return new Notif()
            {
                NotificationId = uuid,
                Content = new Dictionary<string, string>
                {
                    { "en-US", "test content" }
                },
                DisplayIf = new DisplayIf() { ToolkitVersion = "1.40.0.0", Comparison = "<" }
            };
        }
    }
}
