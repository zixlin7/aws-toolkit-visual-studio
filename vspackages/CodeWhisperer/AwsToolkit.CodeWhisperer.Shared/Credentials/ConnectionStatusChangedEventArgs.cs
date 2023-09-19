using System;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public ConnectionStatus ConnectionStatus { get; set; }
    }
}
