﻿using System;
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
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.SourceControl;
using Amazon.AWSToolkit.Tasks;

using AwsToolkit.VsSdk.Common.CommonUI.Models;

using CodeCatalyst.ViewModels;

using Microsoft.VisualStudio.Threading;

namespace CommonUI.Models
{
    internal class CloneCodeCatalystRepositoryViewModel : BaseModel, IDisposable
    {
        private const string HelpUri = "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/codecatalyst.html";

        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IAWSCodeCatalyst _codeCatalyst;
        private readonly IGitService _git;
        private readonly ICredentialIdentifier _awsIdIdentifier =
            new SonoCredentialIdentifier(SonoCredentialProviderFactory.CodeCatalystProfileName);

        private AwsConnectionSettings _connectionSettings;
        private CodeCatalystConnectionStatus _connectionStatus;
        private string _connectionFailure;

        public AwsConnectionSettings ConnectionSettings
        {
            get => _connectionSettings;
            private set => SetProperty(ref _connectionSettings, value);
        }

        /// <summary>
        /// Generalized representation of the current connection state
        /// </summary>
        public CodeCatalystConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            private set => SetProperty(ref _connectionStatus, value);
        }

        /// <summary>
        /// Contains the connection validation text when there is a connection failure
        /// </summary>
        public string ConnectionFailure
        {
            get => _connectionFailure;
            private set => SetProperty(ref _connectionFailure, value);
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

        public bool IsLocalPathEnabled => SelectedRepository != null && Connection.IsConnectionValid;

        private string _localPath;

        public string LocalPath
        {
            get => _localPath;
            set => SetProperty(ref _localPath, value);
        }

        public ICommand BrowseForRepositoryPathCommand { get; }

        public ICommand RetrySpacesCommand { get; }

        public ICommand RetryProjectsCommand { get; }

        public ICommand RetryRepositoriesCommand { get; }

        public ICommand SubmitDialogCommand { get; }

        public ICommand CancelDialogCommand { get; }

        public ICommand HelpCommand { get; }

        public ICommand LogoutCommand { get; }

        public ICommand LoginCommand { get; }

        public string Heading { get; } = "Clone an Amazon CodeCatalyst repository";

        private bool? _dialogResult;

        public bool? DialogResult
        {
            get => _dialogResult;
            private set => SetProperty(ref _dialogResult, value);
        }

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
                CredentialIdentifier = null
            };
            ConnectionManager.ConnectionStateChanged += ConnectionManager_ConnectionStateChanged;
            Connection.PropertyChanged += Connection_PropertyChanged;

            LocalPath = git.GetDefaultRepositoryPath();

            PropertyChanged += CloneCodeCatalystRepositoryViewModel_PropertyChanged;

            BrowseForRepositoryPathCommand = new RelayCommand(ExecuteBrowseForRepositoryPathCommand);
            RetrySpacesCommand = new RelayCommand(parameter => RefreshSpaces());
            RetryProjectsCommand = new RelayCommand(parameter => RefreshProjects());
            RetryRepositoriesCommand = new RelayCommand(parameter => RefreshRepositories());
            HelpCommand = new RelayCommand(ExecuteHelpCommand);
            LoginCommand = new RelayCommand(ExecuteLogin);
            LogoutCommand = new RelayCommand(CanExecuteLogout, ExecuteLogout);
            SubmitDialogCommand = new RelayCommand(CanExecuteSubmitDialog, ExecuteSubmitDialog);
            CancelDialogCommand = new RelayCommand(ExecuteCancelDialog);

            _connectionStatus = CreateDisconnectedConnectionStatus();
        }

        public void Dispose()
        {
            ConnectionManager.ConnectionStateChanged -= ConnectionManager_ConnectionStateChanged;
            Connection.PropertyChanged -= Connection_PropertyChanged;

            Connection.Dispose();
        }

        /// <summary>
        /// Updates connection settings with identifier and region
        /// </summary>
        public void UpdateConnectionSettings()
        {
            ConnectionSettings = new AwsConnectionSettings(Identifier, AwsIdRegion);
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
                    RaiseCanExecuteSubmitDialogChanged();
                    break;
            }
        }

        private void Connection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Connection.CredentialIdentifier))
            {
                OnCredentialIdentifierChanged();
            }
        }

        private void OnCredentialIdentifierChanged()
        {
            UpdateConnectionSettings();
            ConnectionManager.ChangeConnectionSettings(Identifier, AwsIdRegion);
        }

        protected virtual void RaiseCanExecuteSubmitDialogChanged(object parameter = null)
        {
            SubmitDialogCommand?.CanExecute(parameter);
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

        private void ExecuteCancelDialog(object parameter)
        {
            DialogResult = false;
        }

        private void ExecuteSubmitDialog(object parameter)
        {
            DialogResult = true;
        }

        private bool CanExecuteSubmitDialog(object parameter)
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

        public void ExecuteLogin(object parameter)
        {
            // if identifier is not set, set it to AwsBuilderId
            // if identifier is already selected, refresh connection
            if (Connection.CredentialIdentifier == null)
            {
                Connection.CredentialIdentifier = _awsIdIdentifier;
            }
            else
            {
                Connection.ConnectionManager.RefreshConnectionState();
            }
        }

        private bool CanExecuteLogout(object arg)
        {
            return Connection.CredentialIdentifier?.Id == _awsIdIdentifier.Id;
        }

        public void ExecuteLogout(object parameter)
        {
            InvalidateCache();
            Connection.CredentialIdentifier = null;
        }

        public void SetupInitialConnection()
        {
            var isAuthenticated = IsAuthenticated();

            // if already authenticated, set selected identifier for AwsBuilderId that triggers connection validation
            // else, invalidate credential cache and indicate LoginStatus
            if (isAuthenticated)
            {
                Connection.CredentialIdentifier = _awsIdIdentifier;
            }
            else
            {
                ExecuteLogout(null);
            }
        }

        public bool IsAuthenticated()
        {
            var builder = SonoTokenProviderBuilder.Create()
                .WithCredentialIdentifier(_awsIdIdentifier)
                .WithSessionName(SonoCredentialProviderFactory.CodeCatalystSsoSession)
                .WithToolkitShell(_toolkitContext.ToolkitHost);

            return _awsIdIdentifier.HasValidToken(builder);
        }

        public void InvalidateCache()
        {
            _toolkitContext.CredentialManager.Invalidate(_awsIdIdentifier);
        }

        private void ValidateLocalPath()
        {
            try
            {
                DataErrorInfo.ClearErrors(nameof(LocalPath));

                if (!Connection.IsConnectionValid || SelectedRepository == null)
                {
                    return;
                }

                // Using DirectoryInfo as best-effort check for valid path as it throws exceptions on many invalid path scenarios.
                // https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo?view=netframework-4.7.2#remarks
                var dir = new DirectoryInfo(LocalPath);

                if (dir.Exists && (dir.GetDirectories().Any() || dir.GetFiles().Any()))
                {
                    DataErrorInfo.AddError("This path isn't empty. Select or provide a new location", nameof(LocalPath));
                    return;
                }
            }
            catch (Exception ex)
            {
                DataErrorInfo.AddError($"Invalid path: {ex.Message}", nameof(LocalPath));
            }
            finally
            {
                NotifyPropertyChanged(nameof(IsLocalPathEnabled));
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

        private void ConnectionManager_ConnectionStateChanged(object sender, ConnectionStateChangeArgs e)
        {
            _joinableTaskFactory.Run(async () =>
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                var connectionState = e.State;
                Connection.UpdateRequiredConnectionProperties(connectionState);
                ApplyConnectionState(connectionState);
                RefreshSpaces();
            });
        }

        /// <summary>
        /// Updates Connection Status related properties based on the given connection manager state.
        /// </summary>
        public void ApplyConnectionState(ConnectionState connectionState)
        {
            // Reset the Connection Failure - it will be set if needed
            _joinableTaskFactory.Run(async () =>
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ConnectionFailure = string.Empty;
            });

            // Valid connections - show the connected user name
            if (connectionState is ConnectionState.ValidConnection)
            {
                _joinableTaskFactory.RunAsync(async () =>
                {
                    var userName = await GetConnectedUserNameAsync();

                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ConnectionStatus = CreateConnectedConnectionStatus(userName);
                }).Task.LogExceptionAndForget();
            }
            // Validating - indicate that a connection is being established
            else if (connectionState is ConnectionState.ValidatingConnection)
            {
                _joinableTaskFactory.Run(async () =>
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ConnectionStatus = CreateConnectingConnectionStatus();
                });
            }
            // Other Non-Valid state - indicate that we're disconnected
            else
            {
                _joinableTaskFactory.Run(async () =>
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ConnectionStatus = CreateDisconnectedConnectionStatus();

                    // Avoid messages like "no credentials" if user explicitly logs out
                    if (connectionState.IsTerminal)
                    {
                        ConnectionFailure = connectionState.Message;
                    }
                });
            }
        }

        private async Task<string> GetConnectedUserNameAsync()
        {
            var awsId = ConnectionManager.ActiveAwsId;
            return await _codeCatalyst.GetUserNameAsync(awsId, ConnectionSettings);
        }

        public void RefreshSpaces()
        {
            RefreshWithProgressDialog("spaces", async cancellationToken =>
            {
                try
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
                        DataErrorInfo.AddError(ex.Message, nameof(Spaces));
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
                }
                finally
                {
                    NotifyPropertyChanged(nameof(IsSpacesEnabled));
                }
            });
        }

        private void RefreshProjects()
        {
            RefreshWithProgressDialog("projects", async cancellationToken =>
            {
                try
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
                        DataErrorInfo.AddError(ex.Message, nameof(Projects));
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
                }
                finally
                {
                    NotifyPropertyChanged(nameof(IsProjectsEnabled));
                }
            });
        }

        private void RefreshRepositories()
        {
            RefreshWithProgressDialog("repositories", async cancellationToken =>
            {
                try
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
                        var allRepos = await _codeCatalyst.GetRemoteRepositoriesAsync(SelectedSpace.Name,
                            SelectedProject.Name, ConnectionSettings, cancellationToken);
                        // Filter 3p repos: https://sim.amazon.com/issues/IDE-9883
                        repos = await FilterThirdPartyReposAsync(allRepos);
                    }
                    catch (OperationCanceledException)
                    {
                        DataErrorInfo.AddError("Loading repositories canceled by user.", nameof(Repositories));
                        return;
                    }
                    catch (Exception ex)
                    {
                        DataErrorInfo.AddError(ex.Message, nameof(Repositories));
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
                }
                finally
                {
                    NotifyPropertyChanged(nameof(IsRepositoriesEnabled));
                }
            });
        }

        private async Task<IEnumerable<ICodeCatalystRepository>> FilterThirdPartyReposAsync(
            IEnumerable<ICodeCatalystRepository> repos)
        {
            var filteredRepos =
                (await Task.WhenAll(repos.Select(async repo => await IsThirdPartyRepoAsync(repo) ? null : repo)))
                .Where(item => item != null)
                .ToList();
            return filteredRepos;
        }

        private async Task<bool> IsThirdPartyRepoAsync(ICodeCatalystRepository repo)
        {
            var url = await repo.GetCloneUrlAsync(CloneUrlType.Https);
            return !url.Authority.EndsWith("codecatalyst.aws");
        }

        private CodeCatalystConnectionStatus CreateDisconnectedConnectionStatus()
        {
            return new CodeCatalystConnectionStatus()
            {
                Image = _vsImages.Disconnect,
                Text = "Not Connected",
                Command = LoginCommand,
                CommandText = "Connect with AWS Builder ID",
            };
        }

        private CodeCatalystConnectionStatus CreateConnectingConnectionStatus()
        {
            return new CodeCatalystConnectionStatus()
            {
                Image = _vsImages.Loading,
                Text = "Connecting to AWS Builder ID...",
                // In the future, we might set up a cancel command here (requires support from Credential Manager)
            };
        }

        private readonly VsImages _vsImages = new VsImages();
        private CodeCatalystConnectionStatus CreateConnectedConnectionStatus(string userName)
        {
            return new CodeCatalystConnectionStatus()
            {
                Image = _vsImages.StatusOK,
                Text = userName,
                Command = LogoutCommand,
                CommandText = "Log out",
            };
        }
    }
}
