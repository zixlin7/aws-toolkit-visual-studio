using System;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.VisualStudio.Notification;
using Moq;
using Xunit;
using System.Collections.Generic;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.Tests.Common.Settings;

using Notif = Amazon.AWSToolkit.VisualStudio.Notification.Notification;


namespace AWSToolkitPackage.Tests.Notification
{
    public class FakeNotificationInfoBarManager : NotificationInfoBarManager
    {
        public FakeNotificationInfoBarManager(ISettingsRepository<NotificationSettings> settingsRepository,
            IServiceProvider serviceProvider, ToolkitContext toolkitContext,
            string awsProductVersion)
            : base(settingsRepository, serviceProvider, toolkitContext, awsProductVersion)
        {
        }

        public new Task CleanSettingsAsync(IEnumerable<Notif> notifications)
        {
            return base.CleanSettingsAsync(notifications);
        }

        public class NotificationInfoBarManagerTests
        {
            private readonly FakeNotificationInfoBarManager _sut;
            private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

            private readonly FakeSettingsRepository<NotificationSettings> _notificationRepository =
                new FakeSettingsRepository<NotificationSettings>();

            public NotificationInfoBarManagerTests()
            {
                var serviceProvider = new Mock<IServiceProvider>();
                _notificationRepository.Settings = new NotificationSettings() { DismissedNotifications = new List<NotificationSettings.DismissedNotification>() };
                _sut = new FakeNotificationInfoBarManager(_notificationRepository, serviceProvider.Object,
                    _toolkitContextFixture.ToolkitContext,
                    "1.39.0.0");
            }

            [Fact]
            public async Task CleanSettingsAsync_EmptySettingsWithEmptyNotifications()
            {
                // Arrange
                IEnumerable<Notif> notifications = new List<Notif>();

                // Act
                await _sut.CleanSettingsAsync(notifications);

                // Assert
                Assert.Empty(_notificationRepository.Settings.DismissedNotifications);
            }

            [Fact]
            public async Task CleanSettingsAsync_EmptySettingsWithSomeNotifications()
            {
                // Arrange
                IEnumerable<Notif> notifications = new List<Notif>() { BuildNotification("1") };

                // Act
                await _sut.CleanSettingsAsync(notifications);

                // Assert
                Assert.Empty(_notificationRepository.Settings.DismissedNotifications);
            }

            [Fact]
            public async Task CleanSettingsAsync_DismissedIdsExistInHostedFile()
            {
                // Arrange
                IEnumerable<Notif> notifications = new List<Notif>() { BuildNotification("1"), BuildNotification("2") };

                _notificationRepository.Settings.DismissedNotifications.Add(
                    new NotificationSettings.DismissedNotification()
                    {
                        DismissedOn = DateTime.UtcNow.AsUnixMilliseconds(), NotificationId = "1"
                    });

                _notificationRepository.Settings.DismissedNotifications.Add(
                    new NotificationSettings.DismissedNotification()
                    {
                        DismissedOn = DateTime.UtcNow.AsUnixMilliseconds(), NotificationId = "2"
                    });

                // Act
                await _sut.CleanSettingsAsync(notifications);

                // Assert
                Assert.Equal(2, _notificationRepository.Settings.DismissedNotifications.Count);
            }

            [Fact]
            public async Task CleanSettingsAsync_DismissedIdsNotInHostedFileButLessThan2MonthsOld()
            {
                // Arrange
                IEnumerable<Notif> notifications = new List<Notif>();

                _notificationRepository.Settings.DismissedNotifications.Add(
                    new NotificationSettings.DismissedNotification()
                    {
                        DismissedOn = DateTime.UtcNow.AsUnixMilliseconds(), NotificationId = "1"
                    });

                _notificationRepository.Settings.DismissedNotifications.Add(
                    new NotificationSettings.DismissedNotification()
                    {
                        DismissedOn = DateTime.UtcNow.AsUnixMilliseconds(), NotificationId = "2"
                    });

                // Act
                await _sut.CleanSettingsAsync(notifications);

                // Assert
                Assert.Equal(2, _notificationRepository.Settings.DismissedNotifications.Count);
            }

            [Fact]
            public async Task CleanSettingsAsync_DismissedIdsNotInHostedFileAndGreaterThan2MonthsOld()
            {
                // Arrange
                IEnumerable<Notif> notifications = new List<Notif>();

                var dismissalTime = new DateTimeOffset(DateTime.Now.AddMonths(-3)).ToUnixTimeMilliseconds();

                _notificationRepository.Settings.DismissedNotifications.Add(
                    new NotificationSettings.DismissedNotification()
                    {
                        DismissedOn = dismissalTime, NotificationId = "1"
                    });

                _notificationRepository.Settings.DismissedNotifications.Add(
                    new NotificationSettings.DismissedNotification()
                    {
                        DismissedOn = dismissalTime, NotificationId = "2"
                    });

                // Act
                await _sut.CleanSettingsAsync(notifications);

                // Assert
                Assert.Empty(_notificationRepository.Settings.DismissedNotifications);
            }

            [Fact]
            public async Task CleanSettingsAsync_DismissedIdsInHostedFileAndGreaterThan2MonthsOld()
            {
                // Arrange
                IEnumerable<Notif> notifications = new List<Notif>() { BuildNotification("1"), BuildNotification("2") };

                var dismissalTime = new DateTimeOffset(DateTime.Now.AddMonths(-3)).ToUnixTimeMilliseconds();

                _notificationRepository.Settings.DismissedNotifications.Add(
                    new NotificationSettings.DismissedNotification()
                    {
                        DismissedOn = dismissalTime,
                        NotificationId = "1"
                    });

                _notificationRepository.Settings.DismissedNotifications.Add(
                    new NotificationSettings.DismissedNotification()
                    {
                        DismissedOn = dismissalTime,
                        NotificationId = "2"
                    });

                // Act
                await _sut.CleanSettingsAsync(notifications);

                // Assert
                Assert.Equal(2, _notificationRepository.Settings.DismissedNotifications.Count);
            }


            private Notif BuildNotification(string id)
            {
                return new Notif()
                {
                    NotificationId = id,
                    Content = new Dictionary<string, string> { { "en-US", "test en-US content" } },
                    DisplayIf = new DisplayIf() { ToolkitVersion = "1.40.0.0", Comparison = "<" }
                };
            }
        }
    }
}
