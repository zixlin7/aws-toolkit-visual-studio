using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Util;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;
using log4net;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public class CloneRepositoryModel : BaseRepositoryModel
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(CloneRepositoryModel));

        /// <summary>
        /// The service-specific credentials for CodeCommit to be used on the
        /// repository clone.
        /// </summary>
        public ServiceSpecificCredentials ServiceCredentials { get; set; }

        /// <summary>
        /// The folder selected by the user to contain the cloned repository.
        /// </summary>
        public string SelectedFolder
        {
            get { return _selectedFolder; }
            set { _selectedFolder = value; NotifyPropertyChanged("SelectedFolder"); }
        }

        /// <summary>
        /// The https url of the repository selected for cloning. Used in the mode
        /// where we we know the repository in advance.
        /// </summary>
        public string RepositoryUrl { get; set; }

        /// <summary>
        /// Selected repository item from the UI.
        /// </summary>
        public CodeCommitRepository SelectedRepository
        {
            get { return _selectedRepository; }
            set { _selectedRepository = value; NotifyPropertyChanged("SelectedRepository"); }
        }

        public ObservableCollection<CodeCommitRepository> Repositories
        {
            get { return _repositories; }
        }

        public void RefreshRepositoryList()
        {
            RefreshRepositoriesList(GetClientForRegion(SelectedRegion.SystemName));
        }

        private void RefreshRepositoriesList(IAmazonCodeCommit codecommitClient)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);

            ThreadPool.QueueUserWorkItem(x =>
            {
                var repositoryList = new List<RepositoryMetadata>();
                string nextToken = null;
                do
                {
                    try
                    {
                        var response = codecommitClient.ListRepositories(new ListRepositoriesRequest
                        {
                            NextToken = nextToken,
                            SortBy = SortByEnum.LastModifiedDate
                        });

                        nextToken = response.NextToken;

                        foreach (var r in response.Repositories)
                        {
                            var getRepositoryResponse = codecommitClient.GetRepository(new GetRepositoryRequest
                            {
                                RepositoryName = r.RepositoryName
                            });

                            repositoryList.Add(getRepositoryResponse.RepositoryMetadata);
                        }
                    }
                    catch (Exception e)
                    {
                        LOGGER.Error("Failed to retrieve repository list", e);
                        nextToken = null;
                    }
                } while (!string.IsNullOrEmpty(nextToken));

                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                {
                    _repositories.Clear();
                    foreach (var r in repositoryList)
                    {
                        _repositories.Add(new CodeCommitRepository(r));
                    }

                    Interlocked.Decrement(ref _backgroundWorkersActive);
                }));
            });
        }

        private string _selectedFolder;
        private CodeCommitRepository _selectedRepository;
        private readonly ObservableCollection<CodeCommitRepository> _repositories = new ObservableCollection<CodeCommitRepository>();
    }
}
