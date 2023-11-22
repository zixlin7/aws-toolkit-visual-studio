using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Util;

using AwsToolkit.VsSdk.Common.Notifications;

using log4net;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.Notification
{
    internal class NotificationTimer : Timer
    {
        public Notification Notification;
    }

    public class NotificationInfoBarManager : IDisposable
    {
        private const int _maxNotificationsShown = 2;
        private static readonly ILog _logger = LogManager.GetLogger(typeof(NotificationInfoBarManager));
        private const int _setupRetryIntervalMs = 3000;
        private bool _disposed;

        private readonly IServiceProvider _serviceProvider;
        private readonly ToolkitContext _toolkitContext;
        private readonly List<NotificationTimer> _queuedNotificationTimers;
        private readonly FileSettingsRepository<NotificationSettings> _notificationSettingsRepository;
        private readonly NotificationStrategy _strategy;

        public NotificationInfoBarManager(IServiceProvider serviceProvider, ToolkitContext toolkitContext, string awsProductVersion)
        {
            _serviceProvider = serviceProvider;
            _toolkitContext = toolkitContext;
            _notificationSettingsRepository = new FileSettingsRepository<NotificationSettings>(ToolkitAppDataPath.Join("NotificationInfoBarSettings.json"));
            _queuedNotificationTimers = new List<NotificationTimer>();
            _strategy = new NotificationStrategy(_notificationSettingsRepository, _toolkitContext, awsProductVersion, Component.Infobar);

        }

        /// <summary>
        /// Displays an info bar relaying the latest Toolkit Notifications sourced from Hosted Files
        /// </summary>
        public async Task ShowNotificationsAsync(string manifestPath)
        {
            try
            {
                var notificationModel = await FetchNotificationsAsync(manifestPath);

                await CleanSettingsAsync(_notificationSettingsRepository, notificationModel.Notifications);

                await QueueNotificationsAsync(notificationModel.Notifications);

                ShowInfoBars();
            }
            catch (NotificationToolkitException e)
            {
                _strategy.RecordToolkitShowNotificationMetric(Result.Failed, null, e);
            }
        }

        public async Task<NotificationModel> FetchNotificationsAsync(string manifestPath)
        {
            var notificationModelJson = await FetchNotificationModelAsync(manifestPath);
            return DeserializeNotificationModel(notificationModelJson);
        }

        private async Task<string> FetchNotificationModelAsync(string manifestPath)
        {
            try
            {
                return await NotificationUtilities.FetchHttpContentAsStringAsync(manifestPath);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
                throw new NotificationToolkitException("Failed to fetch notification",
                    NotificationToolkitException.NotificationErrorCode.InvalidFetchNotificationRequest, e);
            }
        }

        private NotificationModel DeserializeNotificationModel(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<NotificationModel>(json);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
                throw new NotificationToolkitException("Failed to deserialize notification",
                    NotificationToolkitException.NotificationErrorCode.InvalidNotification, e);
            }
        }

        /// <summary>
        /// Clears cache of dismissed notifications that are over 2 months old and no longer exist in the source hosted file 
        /// </summary>
        private async Task CleanSettingsAsync(FileSettingsRepository<NotificationSettings> settingsRepository,
            IEnumerable<Notification> notifications)
        {
            try
            {
                var settings = await settingsRepository.GetOrDefaultAsync(new NotificationSettings() { DismissedNotifications = new List<NotificationSettings.DismissedNotification>()});

                if (!settings.DismissedNotifications.Any())
                {
                    return;
                }

                var notificationIds = notifications.Select(notification => notification.NotificationId);

                settings.DismissedNotifications = settings.DismissedNotifications.Where(notification =>
                    !notificationIds.Contains(notification.NotificationId) &&
                    IsTimestampOlderThanTwoMonths(notification.DismissedOn)).ToList();

                settingsRepository.Save(settings);
            }
            catch (Exception e)
            {
                const string message = "Failed to clean notification settings";
                _logger.Error(message, e);
                throw new NotificationToolkitException(message,
                    NotificationToolkitException.NotificationErrorCode.InvalidCacheCleanup, e);
            }
        }

        public async Task QueueNotificationsAsync(List<Notification> notifications)
        {
            var notificationDisplayResults = await Task.WhenAll(notifications.Select(async notification => new
            {
                Notification = notification,
                CanShow = await _strategy.CanShowNotificationAsync(notification)
            }));

            var queuedNotifications = notificationDisplayResults.Where(x => x.CanShow).Select(x => x.Notification);

            queuedNotifications
                .OrderByDescending(notification => notification.CreatedOn)
                .Take(_maxNotificationsShown)
                .ToList()
                .ForEach(notification =>
                {
                    var timer = new NotificationTimer()
                    {
                        AutoReset = false,
                        Interval = _setupRetryIntervalMs,
                        Notification = notification
                    };

                    timer.Elapsed += TimerOnElapsed;

                    _queuedNotificationTimers.Add(timer);
                });
        }

        public bool IsTimestampOlderThanTwoMonths(long unixTimeStamp)
        {
            var dateTime = DateTimeUtil.ConvertUnixToDateTime(unixTimeStamp, TimeZoneInfo.Utc);

            var timeSpan = DateTime.UtcNow - dateTime;

            return timeSpan.TotalDays > 60;
        }


        /// <summary>
        /// Responsible for displaying the InfoBar.
        /// Display is performed on a timer, so that attempts
        /// can be performed until it succeeds. (In VS 2019, this
        /// may run before the main window shows, because 2019
        /// shows a project selection dialog first by default)
        /// </summary>
        public void ShowInfoBars()
        {
            _queuedNotificationTimers?.ForEach(timer => timer.Start());
        }

        public void Dispose()
        {
            try
            {
                if (_disposed)
                {
                    return;
                }
                _queuedNotificationTimers.ForEach(timer =>
                {
                    timer.Stop();
                    timer.Elapsed -= TimerOnElapsed;
                    timer.Dispose();
                });
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
            finally
            {
                _disposed = true;
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var timer = (NotificationTimer) sender;

            if (_toolkitContext.ToolkitHost == null)
            {
                timer.Start();
                return;
            }

            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                try
                {
                    AddNotificationInfoBarToMainWindow(timer.Notification);
                }
                catch (Exception exception)
                {
                    timer.Start();
                    _logger.Error(exception);
                }
            });
        }

        private void AddNotificationInfoBarToMainWindow(Notification notification)
        {
            _logger.Debug($"Trying to show notification info bar for {notification.NotificationId}");

            ThreadHelper.ThrowIfNotOnUIThread();

            var infoBarHost = InfoBarUtils.GetMainWindowInfoBarHost(_serviceProvider)
                              ?? throw new Exception("Unable to get main window InfoBar host");

            var notificationInfoBar = new NotificationInfoBar(notification, _strategy, _toolkitContext);

            var notificationInfoBarUiElement = InfoBarUtils.CreateInfoBar(notificationInfoBar.InfoBarModel, _serviceProvider)
                                               ?? throw new Exception($"Unable to create notification info bar parent element for {notification.NotificationId}");

            notificationInfoBar.RegisterInfoBarEvents(notificationInfoBarUiElement);

            infoBarHost.AddInfoBar(notificationInfoBarUiElement);

            _logger.Debug($"Notification info bar displayed for {notification.NotificationId}");

            _strategy.RecordToolkitShowNotificationMetric(Result.Succeeded, notification.NotificationId, null);
        }
    }
}
