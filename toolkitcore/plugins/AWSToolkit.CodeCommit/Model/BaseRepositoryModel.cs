using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Regions;
using Amazon.CodeCommit;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public abstract class BaseRepositoryModel : BaseModel
    {
        protected readonly object _syncLock = new object();
        protected int _backgroundWorkersActive = 0;
        private static readonly string CodeCommitServiceName = new AmazonCodeCommitConfig().RegionEndpointServiceName;

        public bool QueryWorkersActive
        {
            get
            {
                int count;
                lock (_syncLock)
                {
                    count = _backgroundWorkersActive;
                }
                return count != 0;
            }
        }

        public AccountViewModel Account
        {
            get => _account;
            set
            {
                _account = value;
                LoadValidServiceRegionsForAccount();
            }
        }

        public IEnumerable<ToolkitRegion> AvailableRegions => _availableRegions;

        private ToolkitRegion _selectedRegion;
        public ToolkitRegion SelectedRegion
        {
            get => _selectedRegion;
            set
            {
                // Attempt to make the SelectedRegion object an instance from the available regions list,
                // otherwise, select the first region in the list.
                if (_selectedRegion != value)
                {
                    _selectedRegion = _availableRegions.FirstOrDefault(r => r.Id == value?.Id) ??
                                      _availableRegions.FirstOrDefault();
                }
            }
        }

        protected void LoadValidServiceRegionsForAccount()
        {
            _availableRegions.Clear();

            if (this.Account != null)
            {
                var regions = ToolkitFactory.Instance.RegionProvider.GetRegions(this.Account.PartitionId);
                regions
                    .Where(r => ToolkitFactory.Instance.RegionProvider.IsServiceAvailable(CodeCommitServiceName, r.Id))
                    .ToList()
                    .ForEach(r => _availableRegions.Add(r));
            }

            // If SelectedRegion was referenced a different ToolkitRegion instance, "reselect"
            // the one from this list
            if (SelectedRegion != null && !_availableRegions.Contains(SelectedRegion))
            {
                SelectedRegion = _availableRegions.FirstOrDefault(r => r.Id == SelectedRegion.Id);
            }

            if (SelectedRegion == null)
            {
                SelectedRegion = _availableRegions.FirstOrDefault();
            }
        }


        public IAmazonCodeCommit GetClientForRegion(ToolkitRegion region)
        {
            if (!_codeCommitClients.ContainsKey(region.Id))
            {
                var client = GetClientForRegion(Account, region);
                _codeCommitClients.Add(region.Id, client);
            }

            return _codeCommitClients[region.Id];
        }

        internal static IAmazonCodeCommit GetClientForRegion(AccountViewModel account, ToolkitRegion region)
        {
            return account.CreateServiceClient<AmazonCodeCommitClient>(region);
        }

        protected AccountViewModel _account;
        protected readonly List<ToolkitRegion> _availableRegions = new List<ToolkitRegion>();
        private readonly Dictionary<string, IAmazonCodeCommit> _codeCommitClients = new Dictionary<string, IAmazonCodeCommit>();
    }
}
