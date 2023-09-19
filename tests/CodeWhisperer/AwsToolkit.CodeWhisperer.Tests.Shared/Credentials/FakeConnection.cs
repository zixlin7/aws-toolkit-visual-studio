using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Credentials
{
    public class FakeConnection : IConnection
    {
        public ConnectionStatus ConnectionStatus;

        public ConnectionStatus GetStatus()
        {
            return ConnectionStatus;
        }

        public event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;

        public virtual Task SignInAsync()
        {
            ConnectionStatus = ConnectionStatus.Connected;
            return Task.CompletedTask;
        }

        public virtual Task SignOutAsync()
        {
            ConnectionStatus = ConnectionStatus.Disconnected;
            return Task.CompletedTask;
        }

        public void RaiseStatusChanged()
        {
            StatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs()
            {
                ConnectionStatus = ConnectionStatus,
            });
        }
    }
}
