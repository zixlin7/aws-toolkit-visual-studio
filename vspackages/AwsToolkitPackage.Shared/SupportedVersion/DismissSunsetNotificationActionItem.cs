using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.VsSdk.Common.Notifications;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.SupportedVersion
{
    /// <summary>
    /// Records a sunset notification to never be displayed again.
    /// </summary>
    public class DismissSunsetNotificationActionItem : ToolkitInfoBarActionItem
    {
        private readonly ISunsetNotificationStrategy _strategy;

        public DismissSunsetNotificationActionItem(
            ISunsetNotificationStrategy strategy,
            JoinableTaskFactory taskFactory,
            CancellationToken cancellationToken) : base(new InfoBarHyperlink("Don't show this again"), taskFactory, cancellationToken)
        {
            _strategy = strategy;
        }

        public override async Task ExecuteAsync(IVsInfoBarUIElement infoBar)
        {
            await TaskScheduler.Default;
            await _strategy.MarkAsSeenAsync();

            await _taskFactory.SwitchToMainThreadAsync();
            infoBar.Close();
        }
    }
}
