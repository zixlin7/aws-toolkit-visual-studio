using System;
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
        // TODO - Update to appropriate help page once written, see IDE-8830
        private const string HelpUri = "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/using-aws-codecommit-with-team-explorer.html";

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

        private string _localPath;

        public string LocalPath
        {
            get => _localPath;
            set => SetProperty(ref _localPath, value);  // TODO - Shore up path validation (on each change, not leaving control) and handling in IDE-8848
        }

        public ICommand BrowseForRepositoryPathCommand { get; }

        public ICommand SubmitDialogCommand { get; }

        public ICommand HelpCommand { get; }

        public CloneCodeCommitRepositoryViewModel(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory, Action<object> executeSubmitDialogCommand)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;

            PropertyChanged += CloneCodeCommitRepositoryViewModel_PropertyChanged;

            Accounts = new ObservableCollection<AccountViewModel>();
            Regions = new ObservableCollection<ToolkitRegion>();
            Repositories = new ObservableCollection<ICodeCommitRepository>();

            RefreshAccounts();

            BrowseForRepositoryPathCommand = new RelayCommand(ExecuteBrowseForRepositoryPathCommand);
            SubmitDialogCommand = new RelayCommand(CanExecuteSubmitDialogCommand, executeSubmitDialogCommand);
            HelpCommand = new RelayCommand(ExecuteHelpCommand);
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
            dlg.FolderPath = LocalPath;

            if (dlg.ShowModal())
            {
                LocalPath = dlg.FolderPath;
            }
        }

        private bool CanExecuteSubmitDialogCommand(object parameter)
        {
            return !string.IsNullOrWhiteSpace(LocalPath) && SelectedRepository != null;
        }

        public void ExecuteHelpCommand(object parameter)
        {
            _toolkitContext.ToolkitHost.OpenInBrowser(HelpUri, false);
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
