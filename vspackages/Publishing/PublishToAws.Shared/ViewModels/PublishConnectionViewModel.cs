using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Regions;

using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    public enum ConnectionStatus
    {
        Validating,
        Ok,
        Bad,
    }

    /// <summary>
    /// Represents the connection state (Credentials, Region, Validation) of a Publish to AWS view.
    /// </summary>
    public class PublishConnectionViewModel : BaseModel, IPublishToAwsProperties
    {
        private ICredentialIdentifier _credentialIdentifier;

        /// <summary>
        /// The currently selected Credentials
        /// </summary>
        public ICredentialIdentifier CredentialsId
        {
            get => _credentialIdentifier;
            set
            {
                SetProperty(ref _credentialIdentifier, value);
                NotifyPropertyChanged(nameof(CredentialsIdDisplayName));
            }
        }

        public string CredentialsIdDisplayName => CredentialsId?.DisplayName;

        private ToolkitRegion _region;

        /// <summary>
        /// The currently selected Region
        /// </summary>
        public ToolkitRegion Region
        {
            get => _region;
            set
            {
                SetProperty(ref _region, value);
                NotifyPropertyChanged(nameof(RegionDisplayName));
            }
        }

        public string RegionDisplayName => Region?.DisplayName;

        private ConnectionStatus _connectionStatus;

        /// <summary>
        /// The current connection's state
        /// </summary>
        public ConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            set 
            {
                SetProperty(ref _connectionStatus, value);
                _retryConnection.Refresh();
            }
        }

        private string _statusMessage;

        /// <summary>
        /// The current connection's validation message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private ICommand _selectCredentialsCommand;

        /// <summary>
        /// Command that prompts user to select credentials-region pairing
        /// </summary>
        public ICommand SelectCredentialsCommand
        {
            get => _selectCredentialsCommand;
            set => SetProperty(ref _selectCredentialsCommand, value);
        }

        private readonly RelayCommand _retryConnection;
        public ICommand RetryConnection => _retryConnection;

        private readonly IAwsConnectionManager _connectionManager;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        public PublishConnectionViewModel(IAwsConnectionManager connectionManager, JoinableTaskFactory joinableTaskFactory)
        {
            _joinableTaskFactory = joinableTaskFactory;
            _connectionManager = connectionManager;
            _retryConnection = new RelayCommand(_ => CanRetry(), _ => OnRetryConnection());
        }

        private bool CanRetry()
        {
            return ConnectionStatus == ConnectionStatus.Bad;
        }

        private void OnRetryConnection()
        {
            _connectionManager.RefreshConnectionState();
        }

        public void StartListeningToConnectionManager()
        {
            _connectionManager.ConnectionSettingsChanged += ConnectionManagerOnConnectionSettingsChanged;
            _connectionManager.ConnectionStateChanged += ConnectionManagerOnConnectionStateChanged;
        }

        public void StopListeningToConnectionManager()
        {
            _connectionManager.ConnectionStateChanged -= ConnectionManagerOnConnectionStateChanged;
            _connectionManager.ConnectionSettingsChanged -= ConnectionManagerOnConnectionSettingsChanged;
        }

        private void ConnectionManagerOnConnectionSettingsChanged(object sender, ConnectionSettingsChangeArgs e)
        {
            _joinableTaskFactory.Run(async () =>
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                CredentialsId = e.CredentialIdentifier;
                Region = e.Region;
            });
        }

        private void ConnectionManagerOnConnectionStateChanged(object sender, ConnectionStateChangeArgs e)
        {
            var connectionStatus = GetConnectionStatus(e.State);

            _joinableTaskFactory.Run(async () =>
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ConnectionStatus = connectionStatus;
                StatusMessage = e.State.Message;
            });
        }

        private ConnectionStatus GetConnectionStatus(ConnectionState connectionState)
        {
            if (connectionState is ConnectionState.ValidConnection)
            {
                return ConnectionStatus.Ok;
            }

            if (connectionState is ConnectionState.InvalidConnection || connectionState.IsTerminal)
            {
                return ConnectionStatus.Bad;
            }

            return ConnectionStatus.Validating;
        }
    }
}
