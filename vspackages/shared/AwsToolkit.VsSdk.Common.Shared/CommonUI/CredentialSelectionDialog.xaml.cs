using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialSelector;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Settings;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    /// <summary>
    /// Interaction logic for CredentialSelectionDialog.xaml
    /// </summary>
    public partial class CredentialSelectionDialog : DialogWindow, ICredentialSelectionDialog
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        private readonly CredentialSelectionViewModel _viewModel;
        private readonly RelayCommand _okCommand;
        private readonly ICommand _retryValidationCommand;
        private readonly AwsConnectionManager _connectionManager;

        public ICredentialIdentifier CredentialIdentifier
        {
            get => _viewModel.CredentialIdentifier;
            set => _viewModel.CredentialIdentifier = value;
        }

        public ToolkitRegion Region
        {
            get => _viewModel.Region;
            set => _viewModel.Region = value;
        }

        public CredentialSelectionDialog(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;

            _okCommand = CreateOkCommand();
            _retryValidationCommand = CreateRetryValidationCommand();
            _connectionManager = new AwsConnectionManager(AwsConnectionManager.DefaultStsClientCreator,
                _toolkitContext.CredentialManager, _toolkitContext.TelemetryLogger, _toolkitContext.RegionProvider,
                new AppDataToolkitSettingsRepository());
            _viewModel = CreateViewModel();

            InitializeComponent();
            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _connectionManager.ConnectionStateChanged += ConnectionManager_ConnectionStateChanged;
        }

        private CredentialSelectionViewModel CreateViewModel()
        {
            var viewModel = new CredentialSelectionViewModel(_toolkitContext)
            {
                OkCommand = _okCommand, RetryCommand = _retryValidationCommand,
            };
            return viewModel;
        }

        public new bool Show()
        {
            _joinableTaskFactory.Run(async () =>
            {
                await LoadCredentialsAsync();
            });

            var result = ShowModal() ?? false;

            return result;
        }

        private async Task LoadCredentialsAsync()
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();

            _viewModel.Credentials = new ObservableCollection<ICredentialIdentifier>(_toolkitContext.CredentialManager.GetCredentialIdentifiers());
        }

        private void ConnectionManager_ConnectionStateChanged(object sender, ConnectionStateChangeArgs e)
        {
            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                var isConnectionValid = e.State is ConnectionState.ValidConnection;

                _viewModel.ConnectionStatus = _viewModel.GetConnectionStatus(e.State);
                _viewModel.IsConnectionValid = isConnectionValid;
                _viewModel.ConnectionMessage = isConnectionValid ? "Connection is valid" : e.State.Message;
                _viewModel.IsConnectionBad = e.State.IsTerminal && !isConnectionValid;
                _okCommand.Refresh();
            });
        }

        private RelayCommand CreateOkCommand()
        {
            return new RelayCommand(
                _ => _viewModel.IsConnectionValid,
                _ => DialogResult = true);
        }

        private ICommand CreateRetryValidationCommand()
        {
            return new RelayCommand(_ => _connectionManager.RefreshConnectionState());
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Credentials))
            {
                OnCredentialsChanged();
            }
            else if (e.PropertyName == nameof(_viewModel.CredentialIdentifier))
            {
                OnCredentialChanged();
            }
            else if (e.PropertyName == nameof(_viewModel.PartitionId))
            {
                OnPartitionIdChanged();
            }
            else if (e.PropertyName == nameof(_viewModel.Region))
            {
                OnRegionChanged();
            }
        }

        private void OnCredentialsChanged()
        {
            var currentCredentialId = _viewModel.CredentialIdentifier;

            _viewModel.CredentialIdentifier = _viewModel.Credentials.FirstOrDefault(a => a.Id == currentCredentialId.Id);
        }

        private void OnCredentialChanged()
        {
            // Propagate changes to Region (via partition) if the account changed
            _viewModel.PartitionId = _toolkitContext.RegionProvider.GetPartitionId(_viewModel.GetAssociatedRegionId(_viewModel.CredentialIdentifier));
            _connectionManager.ChangeConnectionSettings(_viewModel.CredentialIdentifier, _viewModel.Region);
        }

        private void OnRegionChanged()
        {
            _connectionManager.ChangeConnectionSettings(_viewModel.CredentialIdentifier, _viewModel.Region);
        }

        private void OnPartitionIdChanged()
        {
            var currentRegionId = _viewModel.Region?.Id;

            var regionsView = CollectionViewSource.GetDefaultView(_viewModel.Regions);

            using (regionsView.DeferRefresh())
            {
                _viewModel.ShowRegions(_viewModel.PartitionId);
            }

            // When the Partition changes the list of Regions, the currently selected Region
            // is likely cleared (from databinding).
            // Make a reasonable region selection, if the currently selected region is not available.
            var selectedRegion =
                _viewModel.GetRegion(currentRegionId) ??
                _viewModel.GetRegion(_viewModel.GetMostRecentRegionId(_viewModel.PartitionId)) ??
                _viewModel.Regions.FirstOrDefault();

            _viewModel.Region = selectedRegion;

            regionsView.Refresh();
        }

        public void Dispose()
        {
            _connectionManager?.Dispose();
        }
    }
}
