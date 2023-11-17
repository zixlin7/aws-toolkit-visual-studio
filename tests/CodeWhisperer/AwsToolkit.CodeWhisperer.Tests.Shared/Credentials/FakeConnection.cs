using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Credentials
{
    public class FakeConnection : IConnection
    {
        public ConnectionStatus Status { get; set; }

        public event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;

        public virtual Task SignInAsync()
        {
            Status = ConnectionStatus.Connected;
            return Task.CompletedTask;
        }

        public virtual Task SignOutAsync()
        {
            Status = ConnectionStatus.Disconnected;
            return Task.CompletedTask;
        }

        public void RaiseStatusChanged()
        {
            StatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(Status));
        }
    }
}
