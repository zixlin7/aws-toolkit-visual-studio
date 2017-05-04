using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using Amazon.AWSToolkit.Util;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;
using log4net;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public class CloneRepositoryModel : BaseRepositoryModel
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(CloneRepositoryModel));

        public const string RepoListRefreshStartingPropertyName = "RepoListRefreshStarting";
        public const string RepoListRefreshCompletedPropertyName = "RepoListRefreshCompleted";

        /// <summary>
        /// The service-specific credentials for CodeCommit to be used on the
        /// repository clone.
        /// </summary>
        public ServiceSpecificCredentials ServiceCredentials { get; set; }

        /// <summary>
        /// Contains the initial folder for cloning into, and any selections made
        /// by the user from the browse dialog. The selected repo name will be 
        /// appended to this to form the SelectedFolder value as the user chooses
        /// the repo/
        /// </summary>
        public string BaseFolder
        {
            get { return _baseFolder; }
            set
            {
                _baseFolder = value;
                SelectedFolder = SelectedRepository != null ? Path.Combine(_baseFolder, SelectedRepository.Name) : BaseFolder;
            }
        }

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
            SelectedRepository = null;
            SelectedFolder = BaseFolder;
        }

        /// <summary>
        /// Validates the supplied folder for a clone or create operation and returns a non-empty 
        /// validation failure message that can be displayed to the user if necessary.
        /// </summary>
        /// <returns></returns>
        public static string IsFolderValidForRepo(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return "Folder cannot be empty.";

            try
            {
                var fullpath = Path.GetFullPath(folder); // this should throw on invalid chars etc
                if (Directory.Exists(fullpath))
                {
                    var subdirs = Directory.GetDirectories(fullpath, "*.*", SearchOption.TopDirectoryOnly);
                    var files = Directory.GetFiles(fullpath, "*.*", SearchOption.TopDirectoryOnly);

                    if (subdirs.Length > 0 || files.Length > 0)
                        return "The folder is not empty.";
                }
            }
            catch (Exception)
            {
                return "The folder name is not valid or is not accessible by your account.";
            }

            return null;
        }

        private void RefreshRepositoriesList(IAmazonCodeCommit codecommitClient)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            NotifyPropertyChanged(RepoListRefreshStartingPropertyName);

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
                            SortBy = SortByEnum.RepositoryName,
                            Order = OrderEnum.Ascending
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

                    NotifyPropertyChanged(RepoListRefreshCompletedPropertyName);

                    Interlocked.Decrement(ref _backgroundWorkersActive);
                }));
            });
        }

        private string _baseFolder;
        private string _selectedFolder;
        private CodeCommitRepository _selectedRepository;
        private readonly ObservableCollection<CodeCommitRepository> _repositories = new ObservableCollection<CodeCommitRepository>();
    }
}
