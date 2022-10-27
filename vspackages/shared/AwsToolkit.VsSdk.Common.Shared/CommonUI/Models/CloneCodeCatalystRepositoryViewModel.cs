using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

using Amazon;
using Amazon.AWSToolkit.CodeCatalyst;
using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tasks;

using Microsoft.VisualStudio.Threading;

namespace CommonUI.Models
{
    internal class CloneCodeCatalystRepositoryViewModel : BaseModel
    {
        // TODO - Update to appropriate help page once written, see IDE-8974
        private const string HelpUri = "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/";

        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IAWSCodeCatalyst _codeCatalyst;

        private readonly ToolkitRegion _awsIdRegion;

        private AwsConnectionSettings _connectionSettings;

        public AwsConnectionSettings ConnectionSettings
        {
            get => _connectionSettings;
            private set => SetProperty(ref _connectionSettings, value);
        }

        private ICredentialIdentifier _selectedCredential;

        public ICredentialIdentifier SelectedCredential
        {
            get => _selectedCredential;
            set => SetProperty(ref _selectedCredential, value);
        }
        public ObservableCollection<ICredentialIdentifier> Credentials { get; } = new ObservableCollection<ICredentialIdentifier>();

        private ICodeCatalystSpace _selectedSpace;

        public ICodeCatalystSpace SelectedSpace
        {
            get => _selectedSpace;
            set => SetProperty(ref _selectedSpace, value);
        }

        public ObservableCollection<ICodeCatalystSpace> Spaces { get; } = new ObservableCollection<ICodeCatalystSpace>();

        private ICodeCatalystProject _selectedProject;

        public ICodeCatalystProject SelectedProject
        {
            get => _selectedProject;
            set => SetProperty(ref _selectedProject, value);
        }

        public ObservableCollection<ICodeCatalystProject> Projects { get; } = new ObservableCollection<ICodeCatalystProject>();

        private ICodeCatalystRepository _selectedRepository;

        public ICodeCatalystRepository SelectedRepository
        {
            get => _selectedRepository;
            set => SetProperty(ref _selectedRepository, value);
        }

        public ObservableCollection<ICodeCatalystRepository> Repositories { get; } = new ObservableCollection<ICodeCatalystRepository>();

        private string _localPath;

        public string LocalPath
        {
            get => _localPath;
            set => SetProperty(ref _localPath, value);  // TODO - Shore up path validation (on each change, not leaving control) and handling in IDE-8848
        }

        public ICommand BrowseForRepositoryPathCommand { get; }

        public ICommand SubmitDialogCommand { get; }

        public ICommand HelpCommand { get; }

        public CloneCodeCatalystRepositoryViewModel(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory, Action<object> executeSubmitDialogCommand)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;
            _codeCatalyst = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSCodeCatalyst)) as IAWSCodeCatalyst;

            _awsIdRegion = _toolkitContext.RegionProvider.GetRegion(RegionEndpoint.USEast1.SystemName);

            PropertyChanged += CloneCodeCatalystRepositoryViewModel_PropertyChanged;

            RefreshCredentials();

            BrowseForRepositoryPathCommand = new RelayCommand(ExecuteBrowseForRepositoryPathCommand);
            SubmitDialogCommand = new RelayCommand(CanExecuteSubmitDialogCommand, executeSubmitDialogCommand);
            HelpCommand = new RelayCommand(ExecuteHelpCommand);
        }

        private void CloneCodeCatalystRepositoryViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedCredential):
                    if (SelectedCredential != null)
                    {
                        ConnectionSettings = new AwsConnectionSettings(SelectedCredential, _awsIdRegion);
                    }
                    RefreshSpaces();
                    break;
                case nameof(SelectedSpace):
                    RefreshProjects();
                    break;
                case nameof(SelectedProject):
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

        // TODO - Add ProgressDialog when calling APIs to load view model collections

        private void RefreshCredentials()
        {
            Credentials.Clear();

            // TODO This is just temporary for testing
            var credId = new SonoCredentialIdentifier("default");
            Credentials.Add(credId);

            // TODO Write the actual logic for this IDE-8981
            //CredentialIdentifiers.AddAll(
            //    _toolkitContext.CredentialManager.GetCredentialIdentifiers()
            //    .Where(ci => _toolkitContext.CredentialManager.Supports(ci, AwsConnectionType.AwsToken)));

            //if (SelectedCredentialIdentifier == null)
            //{
            //    SelectedCredentialIdentifier = CredentialIdentifiers.FirstOrDefault();
            //    return;
            //}

            //if (CredentialIdentifiers.Contains(SelectedCredentialIdentifier))
            //{
            //    return;
            //}

            //SelectedCredentialIdentifier = CredentialIdentifiers.FirstOrDefault(i => i.Id == SelectedCredentialIdentifier.Id);
        }

        private void RefreshSpaces()
        {
            Spaces.Clear();

            if (ConnectionSettings == null)
            {
                SelectedSpace = null;
                return;
            }

            _joinableTaskFactory.RunAsync(async () =>
            {
                var spaces = await _codeCatalyst.GetSpacesAsync(ConnectionSettings);
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                Spaces.AddAll(spaces);

                if (SelectedSpace == null)
                {
                    SelectedSpace = Spaces.FirstOrDefault();
                    return;
                }

                if (Spaces.Contains(SelectedSpace))
                {
                    return;
                }

                SelectedSpace = Spaces.FirstOrDefault(s => s.Name == SelectedSpace.Name);
            }).Task.LogExceptionAndForget();
        }

        private void RefreshProjects()
        {
            Projects.Clear();

            if (ConnectionSettings == null || SelectedSpace == null)
            {
                SelectedProject = null;
                return;
            }

            _joinableTaskFactory.RunAsync(async () =>
            {
                var projects = await _codeCatalyst.GetProjectsAsync(SelectedSpace.Name, ConnectionSettings);
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                Projects.AddAll(projects);

                if (SelectedProject == null)
                {
                    SelectedProject = Projects.FirstOrDefault();
                    return;
                }

                if (Projects.Contains(SelectedProject))
                {
                    return;
                }

                SelectedProject = Projects.FirstOrDefault(p => p.Name == SelectedProject.Name);
            }).Task.LogExceptionAndForget();
        }

        private void RefreshRepositories()
        {
            Repositories.Clear();

            if (ConnectionSettings == null || SelectedSpace == null || SelectedProject == null)
            {
                SelectedRepository = null;
                return;
            }

            _joinableTaskFactory.RunAsync(async () =>
            {
                var repos = await _codeCatalyst.GetRemoteRepositoriesAsync(SelectedSpace.Name, SelectedProject.Name, ConnectionSettings);
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                Repositories.AddAll(repos);

                if (SelectedRepository == null)
                {
                    SelectedRepository = Repositories.FirstOrDefault();
                    return;
                }

                if (Repositories.Contains(SelectedRepository))
                {
                    return;
                }

                SelectedRepository = Repositories.FirstOrDefault(r => r.Name == SelectedRepository.Name);
            }).Task.LogExceptionAndForget();
            // TODO - Is LogExceptionAndForget the right thing to do here or should we alert the user to the error?  This has happened to me when
            // my yubi token has expired and it just doens't do anything.  Users may not be using Yubikeys, but it could still be confusing if nothing happens.
            // Look into adding an error message per control or perhaps error messages in a centralized approach
        }
    }
}
