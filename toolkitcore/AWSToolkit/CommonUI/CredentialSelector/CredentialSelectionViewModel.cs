using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CommonUI.CredentialSelector
{
    /// <summary>
    /// This ViewModel contains the functionality and data that
    /// backs the Credential Settings Selection Dialog (<see cref="ICredentialsSelectionDialog"/>)
    /// </summary>
    public class CredentialSelectionViewModel : BaseModel
    {
        private static readonly string DefaultRegionId = RegionEndpoint.USEast1.SystemName;
        /// <summary>
        /// Tracks the most recent non-null region that was used with a partition Id
        /// </summary>
        private readonly Dictionary<string, string> _lastRegionIdPerPartitionId = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _idToRegion = new Dictionary<string, string>();
        private readonly IRegionProvider _regionProvider;
        private readonly ToolkitContext _toolkitContext;
        private ToolkitRegion _region;
        private CredentialConnectionStatus _connectionStatus = CredentialConnectionStatus.Info;
        private ICredentialIdentifier _credentialIdentifier;
        private string _connectionMessage;
        private string _partitionId;
        private ICommand _retryCommand;
        private ICommand _okCommand;
        private bool _isConnectionBad;
        private bool _isConnectionValid;

        private ObservableCollection<ICredentialIdentifier> _credentials =
            new ObservableCollection<ICredentialIdentifier>();

        private ObservableCollection<ToolkitRegion> _regions =
            new ObservableCollection<ToolkitRegion>();

        public CredentialSelectionViewModel(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            _regionProvider = toolkitContext.RegionProvider;
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
        public ICommand RetryCommand
        {
            get => _retryCommand;
            set => SetProperty(ref _retryCommand, value);
        }

        public ICommand OkCommand
        {
            get => _okCommand;
            set => SetProperty(ref _okCommand, value);
        }

        /// <summary>
        /// Updates the <see cref="Regions"/> list with regions contained by the given partitionId.
        /// Side effect: Databinding generally sets <see cref="Region"/> to null as a result.
        /// </summary>
        /// <param name="partitionId">Partition to show regions for</param>
        public void ShowRegions(string partitionId)
        {
            var partitionRegions = _regionProvider.GetRegions(partitionId);

            var regions = partitionRegions
                .OrderBy(r => r.DisplayName)
                .ToList();

            Regions = new ObservableCollection<ToolkitRegion>(regions);
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

        /// <summary>
        /// Retrieves the RegionId most recently used with the queried partition Id.
        /// Returns null if the partition hasn't been used.
        /// </summary>
        public string GetMostRecentRegionId(string partitionId)
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
        public CredentialConnectionStatus GetConnectionStatus(ConnectionState connectionState)
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

        public string GetAssociatedRegionId(ICredentialIdentifier credentialIdentifier)
        {
            if (credentialIdentifier == null)
            {
                return DefaultRegionId;
            }

            if (_idToRegion.TryGetValue(credentialIdentifier.Id, out string regionId))
            {
                return regionId;
            }

            regionId = GetRegionFromProperties(credentialIdentifier);
            _idToRegion[credentialIdentifier.Id] = regionId;
            return regionId;
        }

        private string GetRegionFromProperties(ICredentialIdentifier credentialIdentifier)
        {
            try
            {
                var properties = _toolkitContext.CredentialSettingsManager.GetProfileProperties(credentialIdentifier);
                return string.IsNullOrWhiteSpace(properties.Region) ? DefaultRegionId : properties.Region;
            }
            catch (Exception)
            {
                return DefaultRegionId;
            }
        }
    }
}
