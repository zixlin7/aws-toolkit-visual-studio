using System;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Urls;

using log4net;

namespace Amazon.AWSToolkit.Util
{
    public static class NotificationUtil
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(NotificationUtil));

        public static void ShowMarketplace(ToolkitContext toolkitContext)
        {
            try
            {
                if (toolkitContext.ToolkitHostInfo == ToolkitHosts.Vs2017 ||
                    toolkitContext.ToolkitHostInfo == ToolkitHosts.Vs2019)
                {
                    toolkitContext.ToolkitHost.OpenInBrowser(ServiceUrls.ToolkitMarketplace2019,
                        preferInternalBrowser: false);
                }
                else if (toolkitContext.ToolkitHostInfo == ToolkitHosts.Vs2022)
                {
                    toolkitContext.ToolkitHost.OpenInBrowser(ServiceUrls.ToolkitMarketplace2022,
                        preferInternalBrowser: false);
                }
                else
                {
                    throw new NotificationToolkitException("Toolkit is on an unsupported IDE version",
                        NotificationToolkitException.NotificationErrorCode.UnsupportedIdeVersion);
                }

            }
            catch (NotificationToolkitException toolkitException)
            {
                _logger.Error(toolkitException.Message, toolkitException);
            }
            catch (Exception e)
            {
                const string message = "Failed to open the marketplace page";
                _logger.Error(message, e);
                toolkitContext.ToolkitHost.ShowError("Failed to open URL", $"AWS Toolkit was unable to open the url.\n${e.Message}");
            }
        }
    }
}
