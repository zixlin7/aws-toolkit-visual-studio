using System;

namespace Amazon.AWSToolkit.CommonUI.ToolWindow
{
    /// <summary>
    ///  Toolkit abstraction which produces tool windows
    /// </summary>
    public interface IToolWindowFactory
    {
        void ShowLogGroupsToolWindow(BaseAWSControl control, Func<BaseAWSControl, bool> canUpdateHostedControl);
    }
}
