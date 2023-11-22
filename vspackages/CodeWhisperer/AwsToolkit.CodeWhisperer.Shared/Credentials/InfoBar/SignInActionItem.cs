using System.Threading.Tasks;

using Amazon.AwsToolkit.VsSdk.Common.Notifications;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials.InfoBar
{
    /// <summary>
    /// ActionItem for the "Expired Connection" InfoBar, responsible for taking
    /// user to the sign-in UI.
    /// </summary>
    public class SignInActionItem : ToolkitInfoBarActionItem
    {
        private readonly ICodeWhispererManager _codeWhispererManager;

        public SignInActionItem(
            ICodeWhispererManager codeWhispererManager,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
            : base(new InfoBarButton("Sign in"), taskFactoryProvider.JoinableTaskFactory, taskFactoryProvider.DisposalToken)
        {
            _codeWhispererManager = codeWhispererManager;
        }

        public override async Task ExecuteAsync(IVsInfoBarUIElement infoBar)
        {
            await _codeWhispererManager.SignInAsync();
        }
    }
}
