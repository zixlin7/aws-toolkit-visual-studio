using System;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.ToolWindow;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.VisualStudio.ToolWindow
{
    public class ToolWindowFactory : IToolWindowFactory
    {
        private readonly AWSToolkitPackage _hostPackage;

        public ToolWindowFactory(AWSToolkitPackage hostPackage)
        {
            _hostPackage = hostPackage;
        }

        public void ShowLogGroupsToolWindow(BaseAWSControl control, Func<BaseAWSControl, bool> canUpdateHostedControl)
        {
            _hostPackage.JoinableTaskFactory.Run(async () =>
            {
                await _hostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                // Get the instance number 0 of this tool window. This window is single instance so this instance
                // is actually the only one.
                // The last flag is set to true so that if the tool window does not exists it will be created.
                var window = _hostPackage.FindToolWindow(typeof(LogGroupsToolWindow), 0, true);
                if ((null == window) || (null == window.Frame))
                {
                    throw new NotSupportedException(Resources.CanNotCreateWindow);
                }

                var toolWindow = window as LogGroupsToolWindow;
                if (toolWindow == null)
                {
                    throw new NotSupportedException(
                        $"Unable to create window of expected type: {typeof(LogGroupsToolWindow)}");
                }

                toolWindow.SetChildControl(control, canUpdateHostedControl);

                var windowFrame = (IVsWindowFrame) window.Frame;
                ErrorHandler.ThrowOnFailure(windowFrame.Show());
            });
        }
    }
}
