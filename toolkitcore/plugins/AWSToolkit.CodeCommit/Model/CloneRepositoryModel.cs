using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Util;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;
using Amazon.Runtime.Internal;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public class CloneRepositoryModel : BaseModel
    {
        /// <summary>
        /// The account to use to list available repositories. We will look
        /// for service-specific credentials for CodeCommit on the profile
        /// represented by these credentials to use in the clone operation,
        /// prompting the user to supply them if necessary.
        /// </summary>
        public AccountViewModel Account
        {
            get { return _account; }
            set
            {
                _account = value;
                LoadValidServiceRegionsForAccount();

                var initialRegion = ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints ??
                                    RegionEndPointsManager.Instance.GetRegion("us-east-1");
                SelectedRegion = initialRegion;
            }
        }

        /// <summary>
        /// The service-specific credentials for CodeCommit to be used on the
        /// repository clone.
        /// </summary>
        public ServiceSpecificCredentials ServiceCredentials { get; set; }

        /// <summary>
        /// The folder selected by the user to contain the cloned repository.
        /// </summary>
        public string LocalFolder
        {
            get { return _localFolder; }
            set { _localFolder = value; NotifyPropertyChanged("LocalFolder"); }
        }

        /// <summary>
        /// The https url of the repository selected for cloning. Used in the mode
        /// where we we know the repository in advance.
        /// </summary>
        public string RepositoryUrl { get; set; }

        /// <summary>
        /// Selected repository item from the UI.
        /// </summary>
        public RepositoryWrapper SelectedRepository
        {
            get { return _selectedRepository; }
            set { _selectedRepository = value; NotifyPropertyChanged("SelectedRepository"); }
        }

        public ObservableCollection<RepositoryWrapper> Repositories => _repositories;

        public IEnumerable<RegionEndPointsManager.RegionEndPoints> AvailableRegions => _availableRegions;

        public RegionEndPointsManager.RegionEndPoints SelectedRegion { get; set; }

        public void RefreshRepositoryList()
        {
            _repositories.Clear();
            QueryRepositories(GetClientForRegion(SelectedRegion.SystemName));
        }

        private void QueryRepositories(IAmazonCodeCommit client)
        {
            string nextToken = null;
            do
            {
                client.ListRepositoriesAsync(new ListRepositoriesRequest
                {
                    NextToken = nextToken,
                    SortBy = SortByEnum.LastModifiedDate
                }).ContinueWith(listRepositoriesTask =>
                {
                    if (listRepositoriesTask.Exception == null)
                    {
                        nextToken = listRepositoriesTask.Result.NextToken;

                        foreach (var r in listRepositoriesTask.Result.Repositories)
                        {
                            client.GetRepositoryAsync(new GetRepositoryRequest
                            {
                                RepositoryName = r.RepositoryName
                            }).ContinueWith(getRepositoryTask =>
                            {
                                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                                {
                                    _repositories.Add(new RepositoryWrapper(getRepositoryTask.Result.RepositoryMetadata));
                                }));
                            });
                        }
                    }
                    else
                    {
                        nextToken = null;
                    }
                });
            } while (!string.IsNullOrEmpty(nextToken));
        }

        private IAmazonCodeCommit GetClientForRegion(string region)
        {
            if (!_codeCommitClients.ContainsKey(region))
            {
                var client = new AmazonCodeCommitClient(Account.Credentials, RegionEndpoint.GetBySystemName(region));
                _codeCommitClients.Add(region, client);
            }

            return _codeCommitClients[region];
        }

        private void LoadValidServiceRegionsForAccount()
        {
            _availableRegions.Clear();

            foreach (RegionEndPointsManager.RegionEndPoints rep in RegionEndPointsManager.Instance.Regions)
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
        }

        private AccountViewModel _account;
        private string _localFolder;
        private RepositoryWrapper _selectedRepository;
        private readonly ObservableCollection<RepositoryWrapper> _repositories = new ObservableCollection<RepositoryWrapper>();
        private readonly List<RegionEndPointsManager.RegionEndPoints> _availableRegions = new List<RegionEndPointsManager.RegionEndPoints>();

        private readonly Dictionary<string, IAmazonCodeCommit> _codeCommitClients = new Dictionary<string, IAmazonCodeCommit>();
    }
}
