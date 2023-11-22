using Amazon.AwsToolkit.VsSdk.Common.Tasks;

using AwsToolkit.VsSdk.Common.Notifications;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials.InfoBar
{
    /// <summary>
    /// InfoBar informing users that their CodeWhisperer connection has expired.
    /// </summary>
    public class ConnectionExpiredInfoBar : ToolkitInfoBar
    {
        private readonly IVsInfoBarActionItem _signInActionItem;

        public ConnectionExpiredInfoBar(
            ICodeWhispererManager codeWhispererManager,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _signInActionItem = new SignInActionItem(codeWhispererManager, taskFactoryProvider);

            InfoBarModel = CreateInfoBar();
        }

        protected sealed override InfoBarModel CreateInfoBar()
        {
            var message = "Your connection to Amazon CodeWhisperer has expired. Some features will not be available until you re-connect.";
            return new InfoBarModel(
                textSpans: new[] { new InfoBarTextSpan(message), },
                actionItems: new[] { _signInActionItem },
                image: KnownMonikers.Disconnect,
                isCloseButtonVisible: true);
        }
    }
}
