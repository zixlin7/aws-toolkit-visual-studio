using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;

using CommonUI.Models;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    public partial class CloneCodeCommitRepositoryDialog : DialogWindow, ICloneCodeCommitRepositoryDialog
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly CloneCodeCommitRepositoryViewModel _viewModel;

        public CloneCodeCommitRepositoryDialog(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;
            _viewModel = new CloneCodeCommitRepositoryViewModel(_toolkitContext, _joinableTaskFactory,
                parameter => DialogResult = true);
            InitializeComponent();

            // No window chrome, have to support moving the window ourselves
            MouseDown += (sender, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };

            DataContext = _viewModel;
            Loaded += OnLoaded;

            // This is needed for CredentialsSelector until it is replaced with a modern approach (see CredentialIdentitySelector)
            ThemeUtil.UpdateDictionariesForTheme(Resources);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            Unloaded += OnUnloaded;

            _viewModel.Connection.PropertyChanged += ViewModelConnection_PropertyChanged;
            _viewModel.Connection.ConnectionManager.ConnectionStateChanged += ConnectionManager_ConnectionStateChanged;

            // initialize default selection
            _viewModel.Connection.CredentialIdentifier = _toolkitContext.ConnectionManager.ActiveCredentialIdentifier;
            _viewModel.Connection.Region = _toolkitContext.ConnectionManager.ActiveRegion;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;
            _viewModel.Connection.PropertyChanged -= ViewModelConnection_PropertyChanged;
            _viewModel.Connection.ConnectionManager.ConnectionStateChanged -= ConnectionManager_ConnectionStateChanged;
        }

        private void ConnectionManager_ConnectionStateChanged(object sender, ConnectionStateChangeArgs e)
        {
            _joinableTaskFactory.Run(async () =>
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                var connectionState = e.State;
                _viewModel.Connection.UpdateRequiredConnectionProperties(connectionState);

                if (!connectionState.IsTerminal)
                {
                    return;
                }

                // if connection is valid, reload repositories for updated connection settings
                // else, clear the previously loaded repositories
                if (connectionState is ConnectionState.ValidConnection)
                {
                    _viewModel.RefreshRepositories();
                }
                else
                {
                    _viewModel.Repositories.Clear();
                }
            });
        }

        public string LocalPath { get; private set; }

        public Uri RemoteUri { get; private set; }

        public string RepositoryName { get; private set; }

        public new bool Show()
        {
            LoadCredentials();

            if (ShowModal() != true)
            {
                return false;
            }

            var codeCommitSvc =
                _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSCodeCommit)) as IAWSCodeCommit;
      
            var gitCreds = codeCommitSvc.ObtainGitCredentials(_viewModel.Identifier, _viewModel.Region, false);
            if (gitCreds == null)
            {
                return false;
            }

            LocalPath = _viewModel.LocalPath;
            RepositoryName = _viewModel.SelectedRepository.Name;
            RemoteUri = new UriBuilder(_viewModel.SelectedRepository.RepositoryUrl)
            {
                // See https://github.com/dotnet/runtime/issues/74662 for why we Uri.EscapeDataString username/password
                UserName = Uri.EscapeDataString(gitCreds.Username), Password = Uri.EscapeDataString(gitCreds.Password)
            }.Uri;

            return true;
        }

        private void LoadCredentials()
        {
            _joinableTaskFactory.Run(async () =>
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                _viewModel.Connection.Credentials =
                    new ObservableCollection<ICredentialIdentifier>(_viewModel.Connection.GetCredentialIdentifiers());
            });
        }

        private void ViewModelConnection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Connection.Credentials))
            {
                OnCredentialsChanged();
            }
            else if (e.PropertyName == nameof(_viewModel.Connection.CredentialIdentifier))
            {
                OnCredentialChanged();
            }
            else if (e.PropertyName == nameof(_viewModel.Connection.PartitionId))
            {
                _viewModel.Connection.UpdateRegionForPartition();
            }
            else if (e.PropertyName == nameof(_viewModel.Connection.Region))
            {
                OnRegionChanged();
            }
        }

        private void OnCredentialsChanged()
        {
            var currentCredentialId = _viewModel.Connection.CredentialIdentifier;

            _viewModel.Connection.CredentialIdentifier = currentCredentialId != null
                ? _viewModel.Connection.Credentials.FirstOrDefault(a => a.Id == currentCredentialId.Id)
                : _viewModel.Connection.Credentials.FirstOrDefault();
        }

        private void OnCredentialChanged()
        {
            // Propagate changes to Region (via partition) if the credential changed
            _viewModel.Connection.PartitionId =
                _toolkitContext.RegionProvider.GetPartitionId(
                    _viewModel.Connection.GetAssociatedRegionId(_viewModel.Identifier));
            _viewModel.ConnectionManager.ChangeConnectionSettings(_viewModel.Identifier, _viewModel.Region);
        }

        private void OnRegionChanged()
        {
            _viewModel.ConnectionManager.ChangeConnectionSettings(_viewModel.Identifier, _viewModel.Region);
        }

        public void Dispose()
        {
            _viewModel.Connection?.Dispose();
        }
    }
}
