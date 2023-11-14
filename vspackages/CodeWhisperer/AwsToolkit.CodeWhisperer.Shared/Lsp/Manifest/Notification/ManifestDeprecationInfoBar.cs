using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Notification;
using Amazon.AWSToolkit.Tasks;

using AwsToolkit.VsSdk.Common.Notifications;

using log4net;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest
{
    /// <summary>
    /// Info bar showing the manifest deprecation notice for language server
    /// </summary>
    public class ManifestDeprecationInfoBar : ToolkitInfoBar
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ManifestDeprecationInfoBar));

        private readonly ManifestDeprecationStrategy _manifestDeprecationStrategy;

        private readonly InfoBarHyperlink _infoBarDontShowAgain =
            new InfoBarHyperlink("Don't show this again", ActionContexts.DontShowAgain);

        private readonly InfoBarHyperlink _infoBarMarketplace =
            new InfoBarHyperlink("Update AWS Toolkit", ActionContexts.ShowMarketplace);

        public enum ActionContexts
        {
            ShowMarketplace,
            DontShowAgain
        }

        public ManifestDeprecationInfoBar(ManifestDeprecationStrategy manifestDeprecationStrategy)
        {
            _manifestDeprecationStrategy = manifestDeprecationStrategy;
            InfoBarModel = CreateInfoBar();
        }

        protected sealed override InfoBarModel CreateInfoBar()
        {
            var message =
                "This version of the Toolkit will no longer receive updates to CodeWhisperer Language authoring features.";
            return new InfoBarModel(
                textSpans: new[] { new InfoBarTextSpan(message), },
                actionItems: new[] { _infoBarMarketplace, _infoBarDontShowAgain },
                image: KnownMonikers.StatusInformation,
                isCloseButtonVisible: true);
        }

        protected override void HandleActionItemClicked(IVsInfoBarUIElement infoBarUiElement, IVsInfoBarActionItem actionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (actionItem?.ActionContext is ActionContexts actionContext)
            {
                switch (actionContext)
                {
                    case ActionContexts.DontShowAgain:
                        _manifestDeprecationStrategy.MarkNotificationAsDismissedAsync().LogExceptionAndForget();
                        infoBarUiElement.Close();
                        break;
                    case ActionContexts.ShowMarketplace:
                        _manifestDeprecationStrategy.ShowMarketplaceAsync().LogExceptionAndForget();
                        break;
                }
            }
        }
    }
}
