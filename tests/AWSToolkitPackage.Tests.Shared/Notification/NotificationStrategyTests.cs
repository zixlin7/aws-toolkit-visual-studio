using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Settings;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.AWSToolkit.VisualStudio.Notification;
using Xunit;

using Notif = Amazon.AWSToolkit.VisualStudio.Notification.Notification;

namespace AWSToolkitPackage.Tests.Notification
{
    public class NotificationStrategyTests
    {
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly NotificationStrategy _sut;
        private readonly FileSettingsRepository<NotificationSettings> _notificationRepository;
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private static readonly Notif _sampleNotification = BuildNotification("uuid-1");
        private static readonly Notif _sampleNotification2 = BuildNotification("uuid-2");

        public NotificationStrategyTests()
        {
            var filePath = $@"{_testLocation.InputFolder}\NotificationInfoBarSettings.json";
            _notificationRepository = new FileSettingsRepository<NotificationSettings>(filePath);
            _sut = new NotificationStrategy(_notificationRepository,
                _toolkitContextFixture.ToolkitContext, "1.39.0.0", Component.Infobar);
        }

        [Fact]
        public async Task ShouldMarkNotificationAsDismissed()
        {
            await _sut.MarkNotificationAsDismissedAsync(_sampleNotification);

            var settings = await _notificationRepository.GetOrDefaultAsync(new NotificationSettings());

            Assert.Equal(_sampleNotification.NotificationId, settings.DismissedNotifications.FirstOrDefault()?.NotificationId);
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
