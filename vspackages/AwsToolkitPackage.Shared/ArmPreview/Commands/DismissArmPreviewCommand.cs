using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Notifications;

namespace Amazon.AWSToolkit.VisualStudio.ArmPreview
{
    /// <summary>
    /// Marks the Arm Preview infobar as "don't show again"
    /// </summary>
    public class DismissArmPreviewCommand : AsyncCommand
    {
        private readonly Func<Task> _closeInfoBarAsync;

        public DismissArmPreviewCommand(Func<Task> closeInfoBarAsync)
        {
            _closeInfoBarAsync = closeInfoBarAsync;
        }

        protected override async Task ExecuteCoreAsync(object _)
        {
            ArmPreviewNotice.MarkNoticeAsShown();

            await _closeInfoBarAsync.Invoke();
        }
    }
}
