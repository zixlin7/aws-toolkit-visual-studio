using System;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Urls;
using Amazon.AWSToolkit.Util;
using log4net;


namespace Amazon.AWSToolkit.VisualStudio.Notification
{
    public class ShowMarketplaceAction : INotificationAction
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ShowMarketplaceAction));

        private readonly ToolkitContext _toolkitContext;
        private readonly string _notificationId;

        public bool DisplayAgain { get; set; } = false;
        public bool Dismiss { get; set; } = true;

        public ShowMarketplaceAction(ToolkitContext toolkitContext, string notificationId)
        {
            _toolkitContext = toolkitContext;
            _notificationId = notificationId;
        }

        public Task InvokeAsync(NotificationStrategy strategy)
        {
            ShowMarketplace(strategy);
            return Task.CompletedTask;
        }

        public void ShowMarketplace(NotificationStrategy strategy)
        {
            try
            {
                if (_toolkitContext.ToolkitHostInfo == ToolkitHosts.Vs2017 ||
                    _toolkitContext.ToolkitHostInfo == ToolkitHosts.Vs2019)
                {
                    _toolkitContext.ToolkitHost.OpenInBrowser(ServiceUrls.ToolkitMarketplace2019,
                        preferInternalBrowser: false);
                }
                else if (_toolkitContext.ToolkitHostInfo == ToolkitHosts.Vs2022)
                {
                    _toolkitContext.ToolkitHost.OpenInBrowser(ServiceUrls.ToolkitMarketplace2022,
                        preferInternalBrowser: false);
                }
                else
                {
                    throw new NotificationToolkitException("Toolkit is on an unsupported IDE version",
                        NotificationToolkitException.NotificationErrorCode.UnsupportedIdeVersion);
                }

                RecordToolkitInvokeActionMetric(Result.Succeeded, strategy, null);
            }
            catch (NotificationToolkitException toolkitException)
            {
                _logger.Error(toolkitException.Message, toolkitException);
                RecordToolkitInvokeActionMetric(Result.Failed, strategy, toolkitException);
            }
            catch (Exception e)
            {
                const string message = "Failed to open the marketplace page";
                _logger.Error(message, e);
                _toolkitContext.ToolkitHost.ShowError("Failed to open URL", $"AWS Toolkit was unable to open the url.\n${e.Message}");
                RecordToolkitInvokeActionMetric(Result.Failed, strategy, new ToolkitException(message, ToolkitException.CommonErrorCode.UnexpectedError, e));
            }
        }

        private void RecordToolkitInvokeActionMetric(Result result, NotificationStrategy strategy, ToolkitException e)
        {
            strategy.RecordToolkitInvokeActionMetric(result, ActionContexts.ShowMarketplace, _notificationId, e);
        }
    }
}
