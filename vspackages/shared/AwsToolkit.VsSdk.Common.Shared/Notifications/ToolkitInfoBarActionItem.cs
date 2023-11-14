using System.Threading;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AwsToolkit.VsSdk.Common.Notifications
{
    /// <summary>
    /// Utility class that allows us to associate an (async) handler with a IVsInfoBarActionItem.
    /// This is used as the default "Action clicked" handler in ToolkitInfoBar.
    /// </summary>
    public abstract class ToolkitInfoBarActionItem : IVsInfoBarActionItem
    {
        private readonly IVsInfoBarActionItem _actionItem;
        protected readonly JoinableTaskFactory _taskFactory;
        protected readonly CancellationToken _cancellationToken;

        protected ToolkitInfoBarActionItem(
            IVsInfoBarActionItem actionItem,
            JoinableTaskFactory taskFactory,
            CancellationToken cancellationToken)
        {
            _actionItem = actionItem;
            _taskFactory = taskFactory;
            _cancellationToken = cancellationToken;
        }

        public string Text
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return _actionItem.Text;
            }
        }

        public bool Bold
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return _actionItem.Bold;
            }
        }

        public bool Italic
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return _actionItem.Italic;
            }
        }

        public bool Underline
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return _actionItem.Underline;
            }
        }

        public object ActionContext
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return _actionItem.ActionContext;
            }
        }

        public bool IsButton
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return _actionItem.IsButton;
            }
        }

        public void Execute(IVsInfoBarUIElement infoBar)
        {
            _taskFactory.Run(async () => await ExecuteAsync(infoBar));
        }

        public abstract Task ExecuteAsync(IVsInfoBarUIElement infoBar);
    }
}
