using System;
using System.Threading.Tasks;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    public interface IConnection
    {
        /// <summary>
        /// Gets the current status
        /// </summary>
        ConnectionStatus Status { get; }

        /// <summary>
        /// Event signaling that the status has changed
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Connects the user to CodeWhisperer.
        /// User may go through modal dialogs and login flows as a result.
        /// </summary>
        Task SignInAsync();

        /// <summary>
        /// Signs the user out of CodeWhisperer
        /// </summary>
        Task SignOutAsync();
    }
}
