using System.Threading;

using Amazon.AwsToolkit.VsSdk.Common.Notifications;
using Amazon.AWSToolkit.Context;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.SupportedVersion
{
    /// <summary>
    /// Displays a "Learn More" action link, which opens the provided URL in browser.
    /// </summary>
    public class LearnMoreActionItem : ToolkitInfoBarActionItem
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly string _learnMoreUrl;

        public LearnMoreActionItem(
            string learnMoreUrl,
            ToolkitContext toolkitContext,
            JoinableTaskFactory taskFactory,
            CancellationToken cancellationToken) : base(new InfoBarHyperlink("Learn more"), taskFactory, cancellationToken)
        {
            _toolkitContext = toolkitContext;
            _learnMoreUrl = learnMoreUrl;
        }

        public override async Task ExecuteAsync(IVsInfoBarUIElement infoBar)
        {
            await _taskFactory.SwitchToMainThreadAsync();

            if (!string.IsNullOrWhiteSpace(_learnMoreUrl))
            {
                _toolkitContext.ToolkitHost.OpenInBrowser(_learnMoreUrl, false);
            }

            infoBar.Close();
        }
    }
}
