using System;

using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.SourceControl;

using CommonUI.Models;

using Microsoft.VisualStudio.Threading;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    public partial class CloneCodeCatalystRepositoryDialog : ThemedDialogWindow, ICloneCodeCatalystRepositoryDialog
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly CloneCodeCatalystRepositoryViewModel _viewModel;

        public CloneCodeCatalystRepositoryDialog(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory, IGitService git)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;
            _viewModel = new CloneCodeCatalystRepositoryViewModel(_toolkitContext, _joinableTaskFactory, git);

            InitializeComponent();

            DataContext = _viewModel;
        }

        public AwsConnectionSettings ConnectionSettings { get; private set; }

        public string RepositoryName { get; private set; }

        public Uri CloneUrl { get; private set; }

        public string LocalPath { get; private set; }

        public string UserId { get; private set; }

        public new bool Show()
        {
            using (_viewModel)
            {
                _viewModel.SetupInitialConnection();

                if (ShowModal() != true)
                {
                    return false;
                }

                ConnectionSettings = _viewModel.ConnectionSettings;
                RepositoryName = _viewModel.SelectedRepository.Name;
                LocalPath = _viewModel.LocalPath;
                UserId = _viewModel.ConnectionManager.ActiveAwsId;

                _joinableTaskFactory.Run(async () =>
                {
                    CloneUrl = await _viewModel.SelectedRepository.GetCloneUrlAsync(CloneUrlType.Https);
                });

                return true;
            }
        }
    }
}
