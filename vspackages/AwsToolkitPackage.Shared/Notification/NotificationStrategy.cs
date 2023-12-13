using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Settings;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Exceptions;

namespace Amazon.AWSToolkit.VisualStudio.Notification
{
    public class NotificationStrategy
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(NotificationStrategy));

        private readonly ISettingsRepository<NotificationSettings> _notificationSettingsRepository;
        private readonly ToolkitContext _toolkitContext;
        private readonly Component _component;
        private readonly VersionStrategy _productVersion;
        
        public NotificationStrategy(
            ISettingsRepository<NotificationSettings> notificationSettingsRepository,
            ToolkitContext toolkitContext,
            string productVersion,
            Component component)
        {
            _notificationSettingsRepository = notificationSettingsRepository;
            _toolkitContext = toolkitContext;
            _component = component;
            _productVersion = new VersionStrategy(productVersion);
        }

        public async Task<bool> CanShowNotificationAsync(Notification notification)
        {
            try
            {
                return await _productVersion.IsVersionWithinDisplayConditionsAsync(notification)
                    && !await HasUserDismissedNotificationAsync(notification);
            }
            catch (NotificationToolkitException toolkitException)
            {
                RecordToolkitShowNotificationMetric(Result.Failed, notification.NotificationId, toolkitException);
                return false;
            }
            catch (Exception e)
            {
                RecordToolkitShowNotificationMetric(Result.Failed, notification.NotificationId, new ToolkitException(e.Message, ToolkitException.CommonErrorCode.UnexpectedError, e));
                return false;
            }
        }

        public async Task<bool> HasUserDismissedNotificationAsync(Notification notification)
        {
            try
            {
                var settings = await _notificationSettingsRepository.GetOrDefaultAsync(new NotificationSettings());

                return settings.DismissedNotifications != null
                    && settings.DismissedNotifications.Any()
                    && settings.DismissedNotifications.FirstOrDefault(x =>
                    x.NotificationId == notification.NotificationId) != null;
            }
            catch (Exception e)
            {
                const string message = "Error checking if notification has been dismissed";
                _logger.Error(message, e);
                throw new NotificationToolkitException(message,
                    NotificationToolkitException.NotificationErrorCode.InvalidDismissedNotificationLookup, e);
            }
        }

        public async Task MarkNotificationAsDismissedAsync(Notification notification)
        {
            try
            {
                var settings = await _notificationSettingsRepository.GetOrDefaultAsync(new NotificationSettings());

                if (settings.DismissedNotifications == null)
                {
                    settings.DismissedNotifications = new List<NotificationSettings.DismissedNotification>();
                }

                settings.DismissedNotifications.Add(new NotificationSettings.DismissedNotification()
                {
                    NotificationId = notification.NotificationId,
                    DismissedOn = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });

                _notificationSettingsRepository.Save(settings);
            }
            catch (Exception e)
            {
                const string message = "Error dismissing notification";
                _logger.Error(message, e);
                RecordToolkitShowNotificationMetric(Result.Failed, notification.NotificationId,
                    new NotificationToolkitException(message, NotificationToolkitException.NotificationErrorCode.InvalidNotificationDismissal));
            }
        }

        public void RecordToolkitShowNotificationMetric(Result result, string notificationId, ToolkitException e)
        {
            _toolkitContext.TelemetryLogger.RecordToolkitShowNotification(new ToolkitShowNotification()
            {
                AwsAccount = MetadataValue.NotApplicable,
                AwsRegion = MetadataValue.NotApplicable,
                Id = notificationId ?? MetadataValue.NotSet,
                Result = result,
                Reason = e?.Code ?? MetadataValue.NotApplicable,
                Component = _component,
                Locale = CultureInfo.CurrentCulture.Name
            });
        }

        public void RecordToolkitShowActionMetric(Result result, ActionContexts action, string notificationId, ToolkitException e)
        {
            _toolkitContext.TelemetryLogger.RecordToolkitShowAction(new ToolkitShowAction()
            {
                AwsAccount = MetadataValue.NotApplicable,
                AwsRegion = MetadataValue.NotApplicable,
                Result = result,
                Id = action.ToString(),
                Source = notificationId ?? MetadataValue.NotSet,
                Reason = e?.Code ?? MetadataValue.NotApplicable,
                Component = _component,
                Locale = CultureInfo.CurrentCulture.Name
            });
        }

        public void RecordToolkitInvokeActionMetric(Result result, ActionContexts action, string notificationId, ToolkitException e)
        {
            _toolkitContext.TelemetryLogger.RecordToolkitInvokeAction(new ToolkitInvokeAction()
            {
                AwsAccount = MetadataValue.NotApplicable,
                AwsRegion = MetadataValue.NotApplicable,
                Result = result,
                Id = action.ToString(),
                Source = notificationId ?? MetadataValue.NotSet,
                Reason = e?.Code ?? MetadataValue.NotApplicable,
                Component = _component,
                Locale = CultureInfo.CurrentCulture.Name
            });
        }
    }
}
