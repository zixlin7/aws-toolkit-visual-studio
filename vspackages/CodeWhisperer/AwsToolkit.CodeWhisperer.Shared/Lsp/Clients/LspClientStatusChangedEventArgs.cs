using System;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients
{
    public class LspClientStatusChangedEventArgs : EventArgs
    {
        public LspClientStatusChangedEventArgs(LspClientStatus status)
        {
            ClientStatus = status;
        }

        public LspClientStatus ClientStatus { get; }
    }
}
