using System;
using System.Runtime.InteropServices;

using Amazon.AWSToolkit.CommonUI;

using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// This class implements the tool window exposed by this package
    /// and hosts a user control to view log groups
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid(GuidList.LogGroupsToolWindowGuidString)]
    public class LogGroupsToolWindow : ToolWindowPane
    {
        private readonly LogGroupsToolWindowControl _toolWindowControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogGroupsToolWindow"/> class.
        /// </summary>
        public LogGroupsToolWindow() : base(null)
        {
            this.Caption = "Log Groups Explorer";
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            _toolWindowControl = new LogGroupsToolWindowControl();
            this.Content = _toolWindowControl;
        }

        public void SetChildControl(BaseAWSControl control, Func<BaseAWSControl, bool> canUpdateHostedControl)
        {
            _toolWindowControl.SetChildControl(control, canUpdateHostedControl);
        }
    }
}
