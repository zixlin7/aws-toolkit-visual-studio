using System;
using Amazon.AWSToolkit.Credentials.State;


namespace Amazon.AWSToolkit.Credentials.Utils
{
    /// <summary>
    /// Event arguments that indicate that the connection state of the toolkit has changed
    /// </summary>
    public class ConnectionStateChangeArgs : EventArgs
    {
        public ConnectionState State { get; set; }
    }
}
