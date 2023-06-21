using System;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Context;
using log4net;
using Amazon.AWSToolkit.Exceptions;

namespace Amazon.AWSToolkit.VisualStudio.Notification
{
    public class ShowUrlAction : INotificationAction
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ShowMarketplaceAction));
        private readonly ToolkitContext _toolkitContext;
        private readonly string _notificationId;
        private readonly string _url;

        public bool DisplayAgain { get; set; } = true;
        public bool Dismiss { get; set; } = false;

        public ShowUrlAction(ToolkitContext toolkitContext, string notificationId, string url)
        {
            _toolkitContext = toolkitContext;
            _url = url;
            _notificationId = notificationId;
        }

        public Task InvokeAsync(NotificationStrategy strategy)
        {
            ShowUrl(strategy);
            return Task.CompletedTask;
        }

        public void ShowUrl(NotificationStrategy strategy)
        {
            try
            {
                _toolkitContext.ToolkitHost.OpenInBrowser(_url, preferInternalBrowser: false);
                RecordToolkitInvokeActionMetric(Result.Succeeded, strategy, null);
            }
            catch (Exception e)
            {
                _logger.Error($"Error launching url", e);
                _toolkitContext.ToolkitHost.ShowError("Failed to open URL", $"AWS Toolkit was unable to open ${_url}.\n${e.Message}");
                RecordToolkitInvokeActionMetric(Result.Failed, strategy,
                    new ToolkitException("Failed to open URL", ToolkitException.CommonErrorCode.UnexpectedError, e));
            }
        }

        private void RecordToolkitInvokeActionMetric(Result result, NotificationStrategy strategy, ToolkitException e)
        {
            strategy.RecordToolkitInvokeActionMetric(result, ActionContexts.ShowUrl, _notificationId, e);
        }
    }
}
