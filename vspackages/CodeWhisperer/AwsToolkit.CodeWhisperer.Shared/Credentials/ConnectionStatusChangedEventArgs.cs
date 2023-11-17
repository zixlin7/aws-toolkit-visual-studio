using System;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public ConnectionStatusChangedEventArgs(ConnectionStatus status)
        {
            ConnectionStatus = status;
        }

        public ConnectionStatus ConnectionStatus { get; }
    }
}
