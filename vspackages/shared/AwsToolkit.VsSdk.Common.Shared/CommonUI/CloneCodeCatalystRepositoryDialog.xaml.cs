using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
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
                _viewModel.RefreshConnectedUser(UserId);
                _viewModel.IsConnected = _viewModel.Connection.IsConnectionValid;
                _viewModel.RefreshSpaces();
            });
        }

        public AwsConnectionSettings ConnectionSettings { get; private set; }

        public string RepositoryName { get; private set; }

        public Uri CloneUrl { get; private set; }

        public string LocalPath { get; private set; }

        public string UserId { get; private set; }

        public new bool Show()
        {
            SetupInitialConnection();

            if (ShowModal() != true)
            {
                return false;
            }

            ConnectionSettings = _viewModel.ConnectionSettings;
            RepositoryName = _viewModel.SelectedRepository.Name;
            LocalPath = _viewModel.LocalPath;

            _joinableTaskFactory.Run(async () =>
            {
                CloneUrl = await _viewModel.SelectedRepository.GetCloneUrlAsync(CloneUrlType.Https);
            });

            return true;
        }

        private void SetupInitialConnection()
        {
            _joinableTaskFactory.Run(async () =>
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                _viewModel.SetupInitialConnection();
            });
        }

        private void ViewModelConnection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Connection.CredentialIdentifier))
            {
                OnCredentialIdentifierChanged();
            }
        }

        private void OnCredentialIdentifierChanged()
        {
            _viewModel.UpdateConnectionSettings();
            _viewModel.ConnectionManager.ChangeConnectionSettings(_viewModel.Identifier, _viewModel.AwsIdRegion);
        }
    }
}
