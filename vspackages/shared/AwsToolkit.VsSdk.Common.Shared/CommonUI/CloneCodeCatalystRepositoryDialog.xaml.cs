﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit.CodeCatalyst;
using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.SourceControl;

using AwsToolkit.VsSdk.Common.CommonUI.Commands.CodeCatalyst;

using CommonUI.Models;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    public partial class CloneCodeCatalystRepositoryDialog : DialogWindow, ICloneCodeCatalystRepositoryDialog
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly CloneCodeCatalystRepositoryViewModel _viewModel;

        public CloneCodeCatalystRepositoryDialog(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory, IGitService git)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;
            _viewModel = new CloneCodeCatalystRepositoryViewModel(_toolkitContext, _joinableTaskFactory, git);
            _viewModel.CancelDialogCommand = CancelCloneDialogCommandFactory.Create(this);
            _viewModel.SubmitDialogCommand = SubmitCloneDialogCommandFactory.Create(_viewModel, this);

            InitializeComponent();

            // No window chrome, have to support moving the window ourselves
            MouseDown += (sender, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };

            Loaded += OnLoaded;

            DataContext = _viewModel;

            _viewModel.Connection.PropertyChanged += ViewModelConnection_PropertyChanged;
            _viewModel.Connection.ConnectionManager.ConnectionStateChanged += ConnectionManager_ConnectionStateChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            Unloaded += OnUnloaded;
            // initialize default selection
            _viewModel.Connection.CredentialIdentifier = _viewModel.Connection.Credentials.FirstOrDefault(c => c.FactoryId == SonoCredentialProviderFactory.FactoryId);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;
            _viewModel.Connection.PropertyChanged -= ViewModelConnection_PropertyChanged;
            _viewModel.Connection.ConnectionManager.ConnectionStateChanged -= ConnectionManager_ConnectionStateChanged;
            _viewModel?.Dispose();
        }

        private void ConnectionManager_ConnectionStateChanged(object sender, ConnectionStateChangeArgs e)
        {
            _joinableTaskFactory.Run(async () =>
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                var connectionState = e.State;
                _viewModel.Connection.UpdateRequiredConnectionProperties(connectionState);
                UserId = _viewModel.ConnectionManager.ActiveAwsId;
                if (!connectionState.IsTerminal)
                {
                    return;
                }

                _viewModel.UpdateSpacesForConnectionState(connectionState);
            });
        }

        public AwsConnectionSettings ConnectionSettings { get; private set; }

        public string RepositoryName { get; private set; }

        public Uri CloneUrl { get; private set; }

        public string LocalPath { get; private set; }

        public string UserId { get; private set; }

        public new bool Show()
        {
            LoadCredentials();

            if (ShowModal() != true)
            {
                return false;
            }

            ConnectionSettings = _viewModel.ConnectionSettings;
            RepositoryName = _viewModel.SelectedRepository.Name;
            LocalPath = _viewModel.LocalPath;

            var codeCatalyst = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSCodeCatalyst)) as IAWSCodeCatalyst;

            _joinableTaskFactory.Run(async () =>
            {
                var pat = (await codeCatalyst.GetAccessTokensAsync(_viewModel.ConnectionSettings)).FirstOrDefault() ??
                          (await codeCatalyst.CreateDefaultAccessTokenAsync(null, _viewModel.ConnectionSettings));

                CloneUrl = await _viewModel.SelectedRepository.GetCloneUrlAsync(CloneUrlType.Https, pat);
            });

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
                OnCredentialIdentifierChanged();
            }
        }

        private void OnCredentialsChanged()
        {
            var currentCredentialId = _viewModel.Connection.CredentialIdentifier;

            _viewModel.Connection.CredentialIdentifier = currentCredentialId != null
                ? _viewModel.Connection.Credentials.FirstOrDefault(a => a.Id == currentCredentialId.Id)
                : _viewModel.Connection.Credentials.FirstOrDefault();
        }

        private void OnCredentialIdentifierChanged()
        {
            _viewModel.UpdateConnectionSettings();
            _viewModel.ConnectionManager.ChangeConnectionSettings(_viewModel.Identifier, _viewModel.AwsIdRegion);
        }
    }
}
