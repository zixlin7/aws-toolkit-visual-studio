using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tasks;

using AwsToolkit.VsSdk.Common.CommonUI.Models;

using Microsoft.VisualStudio.Threading;

namespace CommonUI.Models
{
    internal class CloneCodeCommitRepositoryViewModel : BaseModel
    {
        // TODO IDE-9250 Update to appropriate help page once written
        private const string HelpUri = "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/using-aws-codecommit-with-team-explorer.html";

        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
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
            set => SetProperty(ref _localPath, value);  // TODO IDE-8848 Shore up path validation (on each change, not leaving control) and handling
        }

        public ICommand BrowseForRepositoryPathCommand { get; }

        private ICommand _submitDialogCommand;

        public ICommand SubmitDialogCommand
        {
            get => _submitDialogCommand;
            set => SetProperty(ref _submitDialogCommand, value);
        }

        private ICommand _cancelDialogCommand;

        public ICommand CancelDialogCommand
        {
            get => _cancelDialogCommand;
            set => SetProperty(ref _cancelDialogCommand, value);
        }

        public ICommand HelpCommand { get; }

        public string Heading { get; } = "Clone an AWS CodeCommit repository";

        public CloneCodeCommitRepositoryViewModel(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;

            Repositories = new ObservableCollection<ICodeCommitRepository>();

            Connection = new CredentialConnectionViewModel(_toolkitContext)
            {
                ConnectionTypes = new List<AwsConnectionType>() { AwsConnectionType.AwsCredentials },
            };

            BrowseForRepositoryPathCommand = new RelayCommand(ExecuteBrowseForRepositoryPathCommand);
            HelpCommand = new RelayCommand(ExecuteHelpCommand);
        }

        public CredentialConnectionViewModel Connection { get; }

        public ICredentialIdentifier Identifier => Connection.CredentialIdentifier;

        public ToolkitRegion Region => Connection.Region;

        public AwsConnectionManager ConnectionManager => Connection.ConnectionManager;

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

        public bool CanSubmit()
        {
            return Connection.IsConnectionValid && !string.IsNullOrWhiteSpace(LocalPath) && SelectedRepository != null;
        }

        public void ExecuteHelpCommand(object parameter)
        {
            _toolkitContext.ToolkitHost.OpenInBrowser(HelpUri, false);
        }

        public void RefreshRepositories()
        {
            Repositories.Clear();

            if (Identifier == null || Region == null)
            {
                SelectedRepository = null;
                return;
            }

            _joinableTaskFactory.RunAsync(async () =>
            {
                var codeCommitSvc = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSCodeCommit)) as IAWSCodeCommit;
                var repos = await codeCommitSvc.GetRemoteRepositoriesAsync(Identifier,
                    Region);

                await _joinableTaskFactory.SwitchToMainThreadAsync();
                Repositories.AddAll(repos);
            }).Task.LogExceptionAndForget();
        }
    }
}
