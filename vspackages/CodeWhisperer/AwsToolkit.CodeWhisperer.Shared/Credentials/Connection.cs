using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    /// <summary>
    /// Handles the Connection state and operations for CodeWhisperer features.
    /// </summary>
    [Export(typeof(IConnection))]
    internal class Connection : IConnection
    {
        private ConnectionStatus _status = ConnectionStatus.Disconnected;

        /// <summary>
        /// Gets the current status
        /// </summary>
        public ConnectionStatus GetStatus()
        {
            return _status;
        }

        /// <summary>
        /// Event signaling that the status has changed
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Updates the connection status and raises <see cref="StatusChanged"/>
        /// </summary>
        private void SetConnectionStatus(ConnectionStatus status)
        {
            if (_status == status)
            {
                return;
            }

            _status = status;
            StatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs()
            {
                ConnectionStatus = status,
            });
        }

        /// <summary>
        /// Connects the user to CodeWhisperer.
        /// User may go through modal dialogs and login flows as a result.
        /// </summary>
        public Task SignInAsync()
        {
            // TODO : Implement
            SetConnectionStatus(ConnectionStatus.Connected);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Signs the user out of CodeWhisperer
        /// </summary>
        public Task SignOutAsync()
        {
            // TODO : Implement
            SetConnectionStatus(ConnectionStatus.Disconnected);
            return Task.CompletedTask;
        }
    }
}
