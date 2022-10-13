using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tasks;

using Microsoft.VisualStudio.Threading;

namespace CommonUI.Models
{
    internal class CloneCodeCommitRepositoryViewModel : BaseModel
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        private AccountViewModel _selectedAccount;

        public AccountViewModel SelectedAccount
        {
            get => _selectedAccount;
            set => SetProperty(ref _selectedAccount, value);
        }
        public ObservableCollection<AccountViewModel> Accounts { get; }

        private ToolkitRegion _selectedRegion;

        public ToolkitRegion SelectedRegion
        {
            get => _selectedRegion;
            set => SetProperty(ref _selectedRegion, value);
        }

        public ObservableCollection<ToolkitRegion> Regions { get; }

        private ICodeCommitRepository _selectedRepository;

        public ICodeCommitRepository SelectedRepository
        {
            get => _selectedRepository;
            set => SetProperty(ref _selectedRepository, value);
        }

        public ObservableCollection<ICodeCommitRepository> Repositories { get; }

        private string _repositoryPath;

        public string RepositoryPath
        {
            get => _repositoryPath;
            set => SetProperty(ref _repositoryPath, value);
        }

        public ICommand BrowseForRepositoryPathCommand { get; }

        public CloneCodeCommitRepositoryViewModel(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;

            PropertyChanged += CloneCodeCommitRepositoryViewModel_PropertyChanged;

            Accounts = new ObservableCollection<AccountViewModel>();
            Regions = new ObservableCollection<ToolkitRegion>();
            Repositories = new ObservableCollection<ICodeCommitRepository>();

            RefreshAccounts();

            BrowseForRepositoryPathCommand = new RelayCommand(ExecuteBrowseForRepositoryPathCommand);
        }

        private void CloneCodeCommitRepositoryViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedAccount):
                    RefreshRegions();
                    break;
                case nameof(SelectedRegion):
                    RefreshRepositories();
                    break;
            }
        }

        private void ExecuteBrowseForRepositoryPathCommand(object parameter)
        {
            var dlg = _toolkitContext.ToolkitHost.GetDialogFactory().CreateFolderBrowserDialog();
            dlg.Title = "Select folder to clone repository to";
            dlg.FolderPath = RepositoryPath;

            if (dlg.ShowModal())
            {
                RepositoryPath = dlg.FolderPath;
            }
        }

        private void RefreshAccounts()
        {
            Accounts.Clear();

            Accounts.AddAll(ToolkitFactory.Instance.RootViewModel.RegisteredAccounts);

            if (SelectedAccount == null)
            {
                SelectedAccount = Accounts.FirstOrDefault();
                return;
            }

            if (Accounts.Contains(SelectedAccount))
            {
                return;
            }

            SelectedAccount = Accounts.FirstOrDefault(i => i.Identifier == SelectedAccount.Identifier);
        }

        private void RefreshRegions()
        {
            Regions.Clear();

            if (SelectedAccount == null)
            {
                SelectedRegion = null;
                return;
            }
            
            var provider = _toolkitContext.RegionProvider;
            Regions.AddAll(
                provider.GetRegions(SelectedAccount.PartitionId)
                .Where(r => provider.IsServiceAvailable(ServiceNames.CodeCommit, r.Id)));

            if (SelectedRegion == null)
            {
                SelectedRegion = Regions.FirstOrDefault();
                return;
            }

            if (Regions.Contains(SelectedRegion))
            {
                return;
            }

            SelectedRegion = Regions.FirstOrDefault(r => r.Id == SelectedRegion.Id);
        }

        private void RefreshRepositories()
        {
            Repositories.Clear();

            if (SelectedAccount == null || SelectedRegion == null)
            {
                SelectedRepository = null;
                return;
            }

            _joinableTaskFactory.RunAsync(async () =>
            {
                var codeCommitSvc = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSCodeCommit)) as IAWSCodeCommit;
                var repos = await codeCommitSvc.GetRemoteRepositoriesAsync(SelectedAccount, SelectedRegion);

                await _joinableTaskFactory.SwitchToMainThreadAsync();
                Repositories.AddAll(repos);
            }).Task.LogExceptionAndForget();
        }
    }
}
