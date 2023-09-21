using System;

using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;

namespace Amazon.AwsToolkit.CodeWhisperer.Settings
{
    public class CodeWhispererSettingsSavedEventArgs : EventArgs
    {
        public CodeWhispererSettings Settings { get; set; }
    }
}
