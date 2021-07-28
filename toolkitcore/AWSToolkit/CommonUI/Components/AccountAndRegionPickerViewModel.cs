using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Settings;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    public class AccountAndRegionPickerViewModel : BaseModel
    {
        private readonly IRegionProvider _regionProvider;
        private readonly ToolkitContext _toolkitContext;

        /// <summary>
        /// Tracks the most recent non-null region that was used with a partition Id
        /// </summary>
        private readonly Dictionary<string, string> _lastRegionIdPerPartitionId = new Dictionary<string, string>();

        /// <summary>
        /// Indicates which regions are relevant.
        /// Empty list means "show all regions"
        /// Otherwise, only regions that support at least one of the provided services are shown.
        /// Valid values: <see cref="Amazon.Runtime.ClientConfig.RegionEndpointServiceName"/>
        /// </summary>
        private IList<string> _requiredServices = new List<string>();

        private IList<AccountViewModel> _accounts;
        private AccountViewModel _account;
        private string _partitionId;
        private ToolkitRegion _region;
        private ObservableCollection<ToolkitRegion> _regions = new ObservableCollection<ToolkitRegion>();

        // Connection should be initialized to "Not Valid".
        // Assigning Account + Region will trigger validation which would
        // be responsible for getting to a valid state.
        private bool _connectionIsValid = false;
        private bool _connectionIsBad = false;
        private bool _isValidating = false;
        private string _validationMessage;

        /// <summary>
        /// Credentials that can be selected from
        /// </summary>
        public IList<AccountViewModel> Accounts
        {
            get => _accounts;
            set
            {
                SetProperty(ref _accounts, value, () => Accounts);
            }
        }

        /// <summary>
        /// Currently selected credentials
        /// </summary>
        public AccountViewModel Account
        {
            get => _account;
            set
            {
                SetProperty(ref _account, value, () => Account);
            }
        }

        /// <summary>
        /// Whether or not the Account + Region are okay to use
        /// Intermediate validation states (like "Validating") are not considered Valid.
        /// </summary>
        public bool ConnectionIsValid
        {
            get => _connectionIsValid;
            set
            {
                SetProperty(ref _connectionIsValid, value, () => ConnectionIsValid);
            }
        }

        /// <summary>
        /// Indicates that the Account + Region is known as not okay to use.
        /// Intermediate validation states (like "Validating") are not considered Bad.
        /// Bad state examples: Credentials without a region, Credentials not authorized
        /// </summary>
        public bool ConnectionIsBad
        {
            get => _connectionIsBad;
            set
            {
                SetProperty(ref _connectionIsBad, value, () => ConnectionIsBad);
            }
        }

        public bool IsValidating
        {
            get => _isValidating;
            set
            {
                SetProperty(ref _isValidating, value, () => IsValidating);
            }
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                SetProperty(ref _validationMessage, value, () => ValidationMessage);
            }
        }

        /// <summary>
        /// Id of the partition that the "selected account" is associated with.
        /// Example: "aws"
        /// </summary>
        public string PartitionId
        {
            get => _partitionId;
            set
            {
                SetProperty(ref _partitionId, value, () => PartitionId);
            }
        }

        /// <summary>
        /// Currently selected Region
        /// </summary>
        public ToolkitRegion Region
        {
            get => _region;
            set
            {
                SetProperty(ref _region, value, () => Region);

                if (!string.IsNullOrWhiteSpace(_region?.Id) && !string.IsNullOrWhiteSpace(_region?.PartitionId))
                {
                    _lastRegionIdPerPartitionId[_region.PartitionId] = _region.Id;
                }
            }
        }

        /// <summary>
        /// Regions that can be selected in the AWS Explorer
        /// </summary>
        public ObservableCollection<ToolkitRegion> Regions
        {
            get => _regions;
            set
            {
                SetProperty(ref _regions, value, () => Regions);
            }
        }

        public AccountAndRegionPickerViewModel(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            _regionProvider = toolkitContext.RegionProvider;
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
                .Where(x => _requiredServices.Count == 0 || _requiredServices.Any(service => _regionProvider.IsServiceAvailable(service, x.Id)))
                .ToList();

            Regions = new ObservableCollection<ToolkitRegion>(regions);
        }

        /// <summary>
        /// Returns the requested region from <see cref="Regions"/>, or null if not available.
        /// </summary>
        public ToolkitRegion GetRegion(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId))
            { return null; }

            return Regions.FirstOrDefault(r => r.Id == regionId);
        }

        /// <summary>
        /// Retrieves the RegionId most recently used with the queried partition Id.
        /// Returns null if the partition hasn't been used.
        /// </summary>
        public string GetMostRecentRegionId(string partitionId)
        {
            if (string.IsNullOrWhiteSpace(partitionId))
            { return null; }

            if (_lastRegionIdPerPartitionId.TryGetValue(partitionId, out var regionId))
            {
                return regionId;
            }

            return null;
        }

        /// <summary>
        /// Influences which regions are displayed
        /// Empty list means "show all regions"
        /// Otherwise, only regions that support at least one of the provided services are shown.
        /// Valid values: <see cref="Amazon.Runtime.ClientConfig.RegionEndpointServiceName"/>
        /// </summary>
        public void SetServiceFilter(IList<string> serviceNames)
        {
            _requiredServices = new List<string>(serviceNames);
        }

        public void SetRegion(string regionId)
        {
            Region = Regions.FirstOrDefault(r => r.Id == regionId);
        }

        public AwsConnectionManager CreateConnectionManager()
        {
            return new AwsConnectionManager(AwsConnectionManager.DefaultStsClientCreator, _toolkitContext.CredentialManager, _toolkitContext.TelemetryLogger, _toolkitContext.RegionProvider, new AppDataToolkitSettingsRepository());
        }
    }
}
