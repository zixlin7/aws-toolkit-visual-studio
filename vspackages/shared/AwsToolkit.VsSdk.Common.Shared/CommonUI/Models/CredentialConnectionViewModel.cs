using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

using Amazon;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Regions;

using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Settings;

using log4net;

namespace AwsToolkit.VsSdk.Common.CommonUI.Models
{
    public enum CredentialConnectionStatus
    {
        Info, Error, Warning
    }

    public class CredentialConnectionViewModel : BaseModel, IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CredentialConnectionViewModel));

        /// <summary>
        /// Tracks the most recent non-null region that was used with a partition Id
        /// </summary>
        private readonly Dictionary<string, string> _lastRegionIdPerPartitionId = new Dictionary<string, string>();

        private readonly Dictionary<string, string> _idToRegion = new Dictionary<string, string>();
        private readonly IRegionProvider _regionProvider;
        private readonly ToolkitContext _toolkitContext;
        private ToolkitRegion _region;
        private CredentialConnectionStatus _connectionStatus = CredentialConnectionStatus.Info;
        private IList<AwsConnectionType> _connectionTypes = new List<AwsConnectionType>();
        private ICredentialIdentifier _credentialIdentifier;
        private string _connectionMessage;
        private string _partitionId;
        private ICommand _retryCommand;
        private bool _isConnectionBad;
        private bool _isConnectionValid;

        private ObservableCollection<ICredentialIdentifier> _credentials =
            new ObservableCollection<ICredentialIdentifier>();

        private ObservableCollection<ToolkitRegion> _regions =
            new ObservableCollection<ToolkitRegion>();

        public CredentialConnectionViewModel(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            _regionProvider = toolkitContext.RegionProvider;
            ConnectionManager = CreateConnectionManager();
        }

        /// <summary>
        /// AWS Region selected
        /// </summary>
        public ToolkitRegion Region
        {
            get => _region;
            set
            {
                SetProperty(ref _region, value);
                if (!string.IsNullOrWhiteSpace(_region?.Id) && !string.IsNullOrWhiteSpace(_region?.PartitionId))
                {
                    _lastRegionIdPerPartitionId[_region.PartitionId] = _region.Id;
                }
            }
        }

        /// <summary>
        /// AWS partition associated with the region
        /// </summary>
        public string PartitionId
        {
            get => _partitionId;
            set => SetProperty(ref _partitionId, value);
        }

        /// <summary>
        /// AWS partition associated with the region
        /// </summary>
        public AwsConnectionManager ConnectionManager { get; }

        /// <summary>
        /// Whether or not local regions are shown
        /// </summary>
        public bool IncludeLocalRegions = false;

        /// <summary>
        /// Filters the credentials shown in the dialog to the requested connection types.
        /// Leaving this null or empty (default) will show all available credentials.
        /// </summary>
        public IList<AwsConnectionType> ConnectionTypes
        {
            get => _connectionTypes;
            set => SetProperty(ref _connectionTypes, value);
        }

        /// <summary>
        /// The currently selected Credential Id
        /// </summary>
        public ICredentialIdentifier CredentialIdentifier
        {
            get => _credentialIdentifier;
            set => SetProperty(ref _credentialIdentifier, value);
        }

        /// <summary>
        /// Credentials that show up in the edit dialog
        /// </summary>
        public ObservableCollection<ICredentialIdentifier> Credentials
        {
            get => _credentials;
            set => SetProperty(ref _credentials, value);
        }

        /// <summary>
        /// Regions that show up in the edit dialog
        /// </summary>
        public ObservableCollection<ToolkitRegion> Regions
        {
            get => _regions;
            set => SetProperty(ref _regions, value);
        }

        /// <summary>
        /// Indicates if the connection settings are valid
        /// </summary>
        public bool IsConnectionValid
        {
            get => _isConnectionValid;
            set => SetProperty(ref _isConnectionValid, value);
        }

        /// <summary>
        /// Indicates if the connection is bad
        /// </summary>
        public bool IsConnectionBad
        {
            get => _isConnectionBad;
            set => SetProperty(ref _isConnectionBad, value);
        }

        /// <summary>
        /// Indicates connection status used to display status icons
        /// </summary>
        public CredentialConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        /// <summary>
        /// Message displayed with the current connection state
        /// </summary>
        public string ConnectionMessage
        {
            get => _connectionMessage;
            set => SetProperty(ref _connectionMessage, value);
        }

        /// <summary>
        /// Command the retries validation of the connection settings
        /// </summary>
        public ICommand RetryCommand => _retryCommand ?? (_retryCommand = CreateRetryCommand());

        private ICommand CreateRetryCommand()
        {
            return new RelayCommand(_ => ConnectionManager.RefreshConnectionState());
        }

        /// <summary>
        /// Returns the requested region from <see cref="Regions"/>, or null if not available.
        /// </summary>
        public ToolkitRegion GetRegion(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId))
            {
                return null;
            }

            return Regions.FirstOrDefault(r => r.Id == regionId);
        }

        public IEnumerable<ICredentialIdentifier> GetCredentialIdentifiers()
        {
            IEnumerable<ICredentialIdentifier> ids = _toolkitContext.CredentialManager.GetCredentialIdentifiers();

            if (ConnectionTypes != null && ConnectionTypes.Any())
            {
                ids = ids.Where(SupportsConnectionType);
            }

            return ids;
        }

        /// <summary>
        /// Updates region selection for currently selected partition
        /// </summary>
        public void UpdateRegionForPartition()
        {
            var currentRegionId = Region?.Id;

            var regionsView = CollectionViewSource.GetDefaultView(Regions);

            using (regionsView.DeferRefresh())
            {
                ShowRegions(PartitionId);
            }

            // When the Partition changes the list of Regions, the currently selected Region
            // is likely cleared (from databinding).
            // Make a reasonable region selection, if the currently selected region is not available.
            var selectedRegion =
                GetRegion(currentRegionId) ??
                GetRegion(GetMostRecentRegionId(PartitionId)) ??
                Regions.FirstOrDefault();

            Region = selectedRegion;

            regionsView.Refresh();
        }

        public string GetAssociatedRegionId(ICredentialIdentifier credentialIdentifier)
        {
            if (credentialIdentifier == null)
            {
                return ToolkitRegion.DefaultRegionId;
            }

            if (_idToRegion.TryGetValue(credentialIdentifier.Id, out string regionId))
            {
                return regionId;
            }

            regionId = GetRegionFromProperties(credentialIdentifier);
            _idToRegion[credentialIdentifier.Id] = regionId;
            return regionId;
        }

        /// <summary>
        /// Updates connection related properties when connection state changes
        /// </summary>
        public void UpdateRequiredConnectionProperties(ConnectionState state)
        {
            var isConnectionValid = state is ConnectionState.ValidConnection;
            ConnectionStatus = GetConnectionStatus(state);
            IsConnectionValid = isConnectionValid;
            ConnectionMessage = isConnectionValid ? "Connection is valid" : state.Message;
            IsConnectionBad = state.IsTerminal && !isConnectionValid;
        }

        /// <summary>
        /// Updates the <see cref="Regions"/> list with regions contained by the given partitionId.
        /// Side effect: Databinding generally sets <see cref="Region"/> to null as a result.
        /// </summary>
        /// <param name="partitionId">Partition to show regions for</param>
        private void ShowRegions(string partitionId)
        {
            var partitionRegions = _regionProvider.GetRegions(partitionId);

            var regions = partitionRegions
                .Where(r => IncludeLocalRegions || !_regionProvider.IsRegionLocal(r.Id))
                .OrderBy(r => r.DisplayName)
                .ToList();

            Regions = new ObservableCollection<ToolkitRegion>(regions);
        }

        /// <summary>
        /// Retrieves the RegionId most recently used with the queried partition Id.
        /// Returns null if the partition hasn't been used.
        /// </summary>
        private string GetMostRecentRegionId(string partitionId)
        {
            if (string.IsNullOrWhiteSpace(partitionId))
            {
                return null;
            }

            if (_lastRegionIdPerPartitionId.TryGetValue(partitionId, out var regionId))
            {
                return regionId;
            }

            return null;
        }

        /// <summary>
        /// Determines the connection status based on connection state
        /// </summary>
        /// <param name="connectionState">Connection state</param>
        private CredentialConnectionStatus GetConnectionStatus(ConnectionState connectionState)
        {
            if (connectionState is ConnectionState.ValidConnection || !connectionState.IsTerminal)
            {
                return CredentialConnectionStatus.Info;
            }

            if (connectionState is ConnectionState.InvalidConnection)
            {
                return CredentialConnectionStatus.Error;
            }

            return CredentialConnectionStatus.Warning;
        }

        private string GetRegionFromProperties(ICredentialIdentifier credentialIdentifier)
        {
            try
            {
                var properties = _toolkitContext.CredentialSettingsManager.GetProfileProperties(credentialIdentifier);
                return string.IsNullOrWhiteSpace(properties.Region) ? ToolkitRegion.DefaultRegionId : properties.Region;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error occurred while trying to retrieve profile properties for {credentialIdentifier.Id}", ex);
                return ToolkitRegion.DefaultRegionId;
            }
        }

        private bool SupportsConnectionType(ICredentialIdentifier credentialIdentifier)
        {
            return ConnectionTypes.Any(connectionType =>
                _toolkitContext.CredentialManager.Supports(credentialIdentifier, connectionType));
        }

        private AwsConnectionManager CreateConnectionManager()
        {
            return new AwsConnectionManager(
                _toolkitContext.CredentialManager, _toolkitContext.TelemetryLogger,
                _toolkitContext.RegionProvider, new AppDataToolkitSettingsRepository());
        }

        public void Dispose()
        {
            ConnectionManager?.Dispose();
        }
    }
}
