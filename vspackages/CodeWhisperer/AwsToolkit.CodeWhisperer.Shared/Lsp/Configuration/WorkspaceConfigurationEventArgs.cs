using System;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration
{
    public class WorkspaceConfigurationEventArgs : EventArgs
    {
        public object Configuration { get; set; }
    }
}
