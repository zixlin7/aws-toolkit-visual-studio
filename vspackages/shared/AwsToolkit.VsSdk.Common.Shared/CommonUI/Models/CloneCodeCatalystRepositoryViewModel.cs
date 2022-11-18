using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon;
using Amazon.AWSToolkit.CodeCatalyst;
using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.SourceControl;
using Amazon.AWSToolkit.Tasks;

using AwsToolkit.VsSdk.Common.CommonUI.Models;

using Microsoft.VisualStudio.Threading;

using ConnectionState = Amazon.AWSToolkit.Credentials.State.ConnectionState;

namespace CommonUI.Models
{
    internal class CloneCodeCatalystRepositoryViewModel : BaseModel, IDisposable
    {
        // TODO IDE-8974 Update to appropriate help page once written
        private const string HelpUri = "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/";

        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IAWSCodeCatalyst _codeCatalyst;
        private readonly IGitService _git;

        private AwsConnectionSettings _connectionSettings;

        public AwsConnectionSettings ConnectionSettings
        {
            get => _connectionSettings;
            private set => SetProperty(ref _connectionSettings, value);
        }

        public ToolkitRegion AwsIdRegion { get; }

        public CredentialConnectionViewModel Connection { get; }

        public ICredentialIdentifier Identifier => Connection.CredentialIdentifier;

        public AwsConnectionManager ConnectionManager => Connection.ConnectionManager;

        private ICodeCatalystSpace _selectedSpace;

        public ICodeCatalystSpace SelectedSpace
        {
            get => _selectedSpace;
            set => SetProperty(ref _selectedSpace, value);
        }

        public ObservableCollection<ICodeCatalystSpace> Spaces { get; } = new ObservableCollection<ICodeCatalystSpace>();

        public bool IsSpacesEnabled => Spaces.Any() && Connection.IsConnectionValid;

        private ICodeCatalystProject _selectedProject;

        public ICodeCatalystProject SelectedProject
        {
            get => _selectedProject;
            set => SetProperty(ref _selectedProject, value);
        }

        public ObservableCollection<ICodeCatalystProject> Projects { get; } = new ObservableCollection<ICodeCatalystProject>();

        public bool IsProjectsEnabled => SelectedSpace != null && Projects.Any() && Connection.IsConnectionValid;

        private ICodeCatalystRepository _selectedRepository;

        private ICodeCatalystRepository _previousSelectedRepository;

        public ICodeCatalystRepository SelectedRepository
        {
            get => _selectedRepository;
            set
            {
                _previousSelectedRepository = _selectedRepository;
                SetProperty(ref _selectedRepository, value);
            }
        }

        public ObservableCollection<ICodeCatalystRepository> Repositories { get; } = new ObservableCollection<ICodeCatalystRepository>();

        public bool IsRepositoriesEnabled => SelectedProject != null && Repositories.Any() && Connection.IsConnectionValid;

        private string _localPath;

        public string LocalPath
        {
            get => _localPath;
            set => SetProperty(ref _localPath, value);  // TODO - Shore up path validation (on each change, not leaving control) and handling in IDE-8848
        }

        public ICommand BrowseForRepositoryPathCommand { get; }

        public ICommand RetrySpacesCommand { get; }

        public ICommand RetryProjectsCommand { get; }

        public ICommand RetryRepositoriesCommand { get; }

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

        public string Heading { get; } = "Clone an Amazon CodeCatalyst repository";

        public CloneCodeCatalystRepositoryViewModel(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory, IGitService git)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;
            _codeCatalyst = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSCodeCatalyst)) as IAWSCodeCatalyst;
            _git = git;

            AwsIdRegion = _toolkitContext.RegionProvider.GetRegion(RegionEndpoint.USEast1.SystemName);

            Connection = new CredentialConnectionViewModel(_toolkitContext)
            {
                ConnectionTypes = new List<AwsConnectionType>() { AwsConnectionType.AwsToken },
            };

            LocalPath = git.GetDefaultRepositoryPath();

            PropertyChanged += CloneCodeCatalystRepositoryViewModel_PropertyChanged;

            BrowseForRepositoryPathCommand = new RelayCommand(ExecuteBrowseForRepositoryPathCommand);
            RetrySpacesCommand = new RelayCommand(parameter => RefreshSpaces());
            RetryProjectsCommand = new RelayCommand(parameter => RefreshProjects());
            RetryRepositoriesCommand = new RelayCommand(parameter => RefreshRepositories());
            HelpCommand = new RelayCommand(ExecuteHelpCommand);
        }

        public void Dispose()
        {
            Connection.Dispose();
        }

        /// <summary>
        /// Updates connection settings with identifier and region
        /// </summary>
        public void UpdateConnectionSettings()
        {
            if (ConnectionSettings == null)
            {
                ConnectionSettings = new AwsConnectionSettings(Identifier, AwsIdRegion);
            }
            else
            {
                Connection.CredentialIdentifier = Identifier;
            }
        }

        private void CloneCodeCatalystRepositoryViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedSpace):
                    RefreshProjects();
                    break;
                case nameof(SelectedProject):
                    RefreshRepositories();
                    break;
                case nameof(SelectedRepository):
                    TryAutoCompleteLocalPath();
                    break;
                case nameof(LocalPath):
                    ValidateLocalPath();
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

        public bool CanSubmit()
        {
            return
                Connection.IsConnectionValid &&
                !string.IsNullOrWhiteSpace(LocalPath) &&
                SelectedRepository != null &&
                !Enumerable.Cast<object>(DataErrorInfo.GetErrors(null)).Any();
        }

        public void ExecuteHelpCommand(object parameter)
        {
            _toolkitContext.ToolkitHost.OpenInBrowser(HelpUri, false);
        }

        private void ValidateLocalPath()
        {
            DataErrorInfo.ClearErrors(nameof(LocalPath));

            try
            {
                // Using DirectoryInfo as best-effort check for valid path as it throws exceptions on many invalid path scenarios.
                // https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo?view=netframework-4.7.2#remarks
                var dir = new DirectoryInfo(LocalPath);

                if (dir.Exists && (dir.GetDirectories().Any() || dir.GetFiles().Any()))
                {
                    DataErrorInfo.AddError($"Cannot clone to existing, non-empty directory {LocalPath}", nameof(LocalPath));
                    return;
                }
            }
            catch (Exception ex)
            {
                DataErrorInfo.AddError($"Invalid path: {ex.Message}", nameof(LocalPath));
            }
        }

        private string RemoveInvalidPathChars(string path)
        {
            foreach (char invalidChar in Path.GetInvalidPathChars())
            {
                path = path.Replace(invalidChar.ToString(), string.Empty);
            }

            return path;
        }

        private void TryAutoCompleteLocalPath()
        {
            // Rudimentary and not as sophisticated as GitHub's provider, but to perform that level of path rewriting,
            // we'd have to track every character/selection change in the path.

            if (string.IsNullOrWhiteSpace(LocalPath))
            {
                LocalPath = _git.GetDefaultRepositoryPath();
            }

            var previousRepoName = RemoveInvalidPathChars(_previousSelectedRepository?.Name ?? string.Empty);
            var selectedRepoName = RemoveInvalidPathChars(SelectedRepository?.Name ?? string.Empty);

            if (string.Empty == previousRepoName)
            {
                LocalPath = Path.Combine(LocalPath, selectedRepoName);
                return;
            }

            if (LocalPath.EndsWith($"{Path.DirectorySeparatorChar}{previousRepoName}"))
            {
                var root = LocalPath.Substring(0, LocalPath.LastIndexOf(Path.DirectorySeparatorChar));
                LocalPath = Path.Combine(root, selectedRepoName);
                return;
            }
        }

        /// <summary>
        /// Update spaces for the given connection state
        /// if connection is valid, reload spaces
        /// else, clear the previously loaded spaces
        /// </summary>
        public void UpdateSpacesForConnectionState(ConnectionState connectionState)
        {
            if (connectionState is ConnectionState.ValidConnection)
            {
                RefreshSpaces();
            }
            else
            {
                Spaces.Clear();
            }
        }

        private void RefreshWithProgressDialog(string name, Func<CancellationToken, Task> refreshAsync)
        {
            _joinableTaskFactory.RunAsync(async () =>
            {
                using (var dialog = await _toolkitContext.ToolkitHost.CreateProgressDialog())
                {
                    dialog.Caption = "Amazon CodeCatalyst";
                    dialog.Heading1 = $"Loading {name}...";
                    dialog.Show(1);

                    await refreshAsync(dialog.CancellationToken);
                }
            }).Task.LogExceptionAndForget();
        }

        private void RefreshSpaces()
        {
            RefreshWithProgressDialog("spaces", async cancellationToken =>
            {
                Spaces.Clear();
                DataErrorInfo.ClearErrors(nameof(Spaces));

                if (!Connection.IsConnectionValid)
                {
                    SelectedSpace = null;
                    return;
                }

                IEnumerable<ICodeCatalystSpace> spaces;

                try
                {
                    spaces = await _codeCatalyst.GetSpacesAsync(ConnectionSettings, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    DataErrorInfo.AddError("Loading spaces canceled by user.", nameof(Spaces));
                    return;
                }
                catch (Exception ex)
                {
                    DataErrorInfo.AddError(ex, nameof(Spaces));
                    return;
                }

                await _joinableTaskFactory.SwitchToMainThreadAsync();
                Spaces.AddAll(spaces);

                if (SelectedSpace == null)
                {
                    SelectedSpace = Spaces.FirstOrDefault();
                }
                else if (!Spaces.Contains(SelectedSpace))
                {
                    SelectedSpace = Spaces.FirstOrDefault(s => s.Name == SelectedSpace.Name);
                }

                NotifyPropertyChanged(nameof(IsSpacesEnabled));
            });
        }

        private void RefreshProjects()
        {
            RefreshWithProgressDialog("projects", async cancellationToken =>
            {
                Projects.Clear();
                DataErrorInfo.ClearErrors(nameof(Projects));

                if (!Connection.IsConnectionValid || SelectedSpace == null)
                {
                    SelectedProject = null;
                    return;
                }

                IEnumerable<ICodeCatalystProject> projects;

                try
                {
                    projects = await _codeCatalyst.GetProjectsAsync(SelectedSpace.Name, ConnectionSettings, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    DataErrorInfo.AddError("Loading projects canceled by user.", nameof(Projects));
                    return;
                }
                catch (Exception ex)
                {
                    DataErrorInfo.AddError(ex, nameof(Projects));
                    return;
                }

                await _joinableTaskFactory.SwitchToMainThreadAsync();
                Projects.AddAll(projects);

                if (SelectedProject == null)
                {
                    SelectedProject = Projects.FirstOrDefault();
                }
                else if (!Projects.Contains(SelectedProject))
                {
                    SelectedProject = Projects.FirstOrDefault(p => p.Name == SelectedProject.Name);
                }

                NotifyPropertyChanged(nameof(IsProjectsEnabled));
            });
        }

        private void RefreshRepositories()
        {
            RefreshWithProgressDialog("repositories", async cancellationToken =>
            {
                Repositories.Clear();
                DataErrorInfo.ClearErrors(nameof(Repositories));

                if (!Connection.IsConnectionValid || SelectedProject == null)
                {
                    SelectedRepository = null;
                    return;
                }

                IEnumerable<ICodeCatalystRepository> repos;

                try
                {
                    repos = await _codeCatalyst.GetRemoteRepositoriesAsync(SelectedSpace.Name, SelectedProject.Name, ConnectionSettings, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    DataErrorInfo.AddError("Loading repositories canceled by user.", nameof(Repositories));
                    return;
                }
                catch (Exception ex)
                {
                    DataErrorInfo.AddError(ex, nameof(Repositories));
                    return;
                }

                await _joinableTaskFactory.SwitchToMainThreadAsync();
                Repositories.AddAll(repos);

                if (SelectedRepository == null)
                {
                    SelectedRepository = Repositories.FirstOrDefault();
                }
                else if (!Repositories.Contains(SelectedRepository))
                {
                    SelectedRepository = Repositories.FirstOrDefault(r => r.Name == SelectedRepository.Name);
                }

                NotifyPropertyChanged(nameof(IsRepositoriesEnabled));
            });
        }
    }
}
