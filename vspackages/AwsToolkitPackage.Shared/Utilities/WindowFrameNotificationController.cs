using Amazon.AWSToolkit.Shared;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.VisualStudio.Utilities
{
    /// <summary>
    /// Toolkit abstraction around VSSDK Window Frame Notifications
    /// </summary>
    public class WindowFrameNotificationController : IVsWindowFrameNotify2
    {
        private IAWSToolkitControl _control;

        public WindowFrameNotificationController(IAWSToolkitControl control)
        {
            _control = control;
        }

        public int OnClose(ref uint pgrfSaveOptions)
        {
            return _control.CanClose() ? VSConstants.S_OK : VSConstants.E_ABORT;
        }
    }
}
