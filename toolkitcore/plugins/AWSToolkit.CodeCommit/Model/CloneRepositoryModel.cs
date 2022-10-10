using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.CodeCommit.Util;
using Amazon.AWSToolkit.Util;
using Amazon.CodeCommit;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public class CloneRepositoryModel : BaseRepositoryModel
    {
        public const string RepoListRefreshStartingPropertyName = "RepoListRefreshStarting";
        public const string RepoListRefreshCompletedPropertyName = "RepoListRefreshCompleted";

        private string _baseFolder;
        private string _selectedFolder;
        private CodeCommitRepository _selectedRepository;
        private readonly RangeObservableCollection<ICodeCommitRepository> _repositories = new RangeObservableCollection<ICodeCommitRepository>();

        private readonly SortOption _sortByRepositoryName =
            new SortOption
            {
                SortBy = SortByEnum.RepositoryName,
                DisplayText = "Repository Name",
                SortMethod = (codeCommitRepository) => codeCommitRepository.Name
            };

        private readonly SortOption _sortByLastModifiedDate =
            new SortOption
            {
                SortBy = SortByEnum.LastModifiedDate,
                DisplayText = "Last Modified Date",
                SortMethod = (codeCommitRepository) => codeCommitRepository.LastModifiedDate
            };

        private readonly OrderOption _orderAscending =
            new OrderOption { Order = OrderEnum.Ascending, DisplayText = "Ascending" };

        private readonly OrderOption _orderDescending =
            new OrderOption { Order = OrderEnum.Descending, DisplayText = "Descending" };


        public CloneRepositoryModel()
        {
            SortBy = _sortByRepositoryName;
            Order = _orderAscending;
        }

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
            get => _baseFolder;
            set
            {
                SetProperty(ref _baseFolder, value);
                SelectedFolder = SelectedRepository != null ? Path.Combine(_baseFolder, SelectedRepository.Name) : BaseFolder;
            }
        }

        /// <summary>
        /// The folder selected by the user to contain the cloned repository.
        /// </summary>
        public string SelectedFolder
        {
            get => _selectedFolder;
            set => SetProperty(ref _selectedFolder, value);
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
            get => _selectedRepository;
            set => SetProperty(ref _selectedRepository, value);
        }

        public RangeObservableCollection<ICodeCommitRepository> Repositories => _repositories;

        public List<SortOption> SortByOptions => new List<SortOption> {_sortByRepositoryName, _sortByLastModifiedDate};

        public SortOption SortBy { get; set; }

        public List<OrderOption> OrderOptions => new List<OrderOption> {_orderAscending, _orderDescending};

        public OrderOption Order { get; set; }

        public void RefreshRepositoryList()
        {
            SelectedRepository = null;
            SelectedFolder = BaseFolder;
            RefreshRepositoriesList(GetClientForRegion(SelectedRegion));
        }

        /// <summary>
        /// Validates the supplied folder for a clone or create operation and returns a non-empty 
        /// validation failure message that can be displayed to the user if necessary.
        /// </summary>
        /// <returns></returns>
        public static string IsFolderValidForRepo(string folder)
        {
            if (string.IsNullOrEmpty(folder))
            {
                return "Folder name cannot be empty.";
            }

            try
            {
                var fullpath = Path.GetFullPath(folder); // this should throw on invalid chars etc
                if (Directory.Exists(fullpath))
                {
                    if (!Directory.EnumerateFileSystemEntries(fullpath).Any())
                    {
                        return "The folder is not empty.";
                    }
                }
            }
            catch (Exception)
            {
                return "The folder name is not valid or is not accessible by your account.";
            }

            return null;
        }

        private void RefreshRepositoriesList(IAmazonCodeCommit codeCommitClient)
        {
            NotifyPropertyChanged(RepoListRefreshStartingPropertyName);

            ThreadPool.QueueUserWorkItem(async x =>
            {
                var repositoryList = await LoadRepositoryModels(codeCommitClient);

                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    _repositories.Clear();
                    _repositories.AddRange(repositoryList);

                    NotifyPropertyChanged(RepoListRefreshCompletedPropertyName);
                }));
            });
        }

        private async Task<IEnumerable<ICodeCommitRepository>> LoadRepositoryModels(IAmazonCodeCommit codeCommitClient)
        {
            var repositoryNames = await codeCommitClient.ListRepositoryNames();
            var repositoryMetadata = await codeCommitClient.GetRepositoryMetadata(repositoryNames);

            return SortAndOrder(repositoryMetadata.Select(metadata => new CodeCommitRepository(metadata)));
        }

        /// <summary>
        /// Sorts and orders the given repository metadata based on this model's settings
        /// </summary>
        /// <param name="metadata">metadata to sort/order</param>
        /// <returns>sorted/ordered metadata enumerable</returns>
        private IEnumerable<ICodeCommitRepository> SortAndOrder(IEnumerable<ICodeCommitRepository> codeCommitRepositories)
        {
            return Order == _orderAscending ?
                codeCommitRepositories.OrderBy(SortBy.SortMethod) :
                codeCommitRepositories.OrderByDescending(SortBy.SortMethod);
        }
    }

    public class SortOption
    {
        public SortByEnum SortBy { get; set; }
        public string DisplayText { get; set; }
        public Func<ICodeCommitRepository, object> SortMethod { get; set; }
    }

    public class OrderOption
    {
        public OrderEnum Order { get; set; }
        public string DisplayText { get; set; }
    }
}
