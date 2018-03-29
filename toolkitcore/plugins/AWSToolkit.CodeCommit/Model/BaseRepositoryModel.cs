using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.CodeCommit;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public abstract class BaseRepositoryModel : BaseModel
    {
        protected readonly object _syncLock = new object();
        protected int _backgroundWorkersActive = 0;

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
            get { return _account; }
            set
            {
                _account = value;
                LoadValidServiceRegionsForAccount();
            }
        }

        public IEnumerable<RegionEndPointsManager.RegionEndPoints> AvailableRegions
        {
            get { return _availableRegions; }
        }

        public RegionEndPointsManager.RegionEndPoints SelectedRegion { get; set; }

        protected void LoadValidServiceRegionsForAccount()
        {
            _availableRegions.Clear();

            foreach (RegionEndPointsManager.RegionEndPoints rep in RegionEndPointsManager.GetInstance().Regions)
            {
                if (this.Account.HasRestrictions || rep.HasRestrictions)
                {
                    if (!rep.ContainAnyRestrictions(this.Account.Restrictions))
                    {
                        continue;
                    }
                }

                if (rep.GetEndpoint(RegionEndPointsManager.CODECOMMIT_SERVICE_NAME) != null)
                {
                    _availableRegions.Add(rep);
                }
            }

            SelectedRegion = _availableRegions.FirstOrDefault();
        }

        public IAmazonCodeCommit GetClientForRegion(string region)
        {
            if (!_codeCommitClients.ContainsKey(region))
            {
                var client = GetClientForRegion(Account.Credentials, region);
                _codeCommitClients.Add(region, client);
            }

            return _codeCommitClients[region];
        }

        internal static IAmazonCodeCommit GetClientForRegion(AWSCredentials credentials, string region)
        {
            return new AmazonCodeCommitClient(credentials, RegionEndpoint.GetBySystemName(region));
        }

        protected AccountViewModel _account;
        protected readonly List<RegionEndPointsManager.RegionEndPoints> _availableRegions = new List<RegionEndPointsManager.RegionEndPoints>();
        private readonly Dictionary<string, IAmazonCodeCommit> _codeCommitClients = new Dictionary<string, IAmazonCodeCommit>();
    }
}
