using System;
using System.Windows;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;

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
            _viewModel = new CloneCodeCommitRepositoryViewModel(_toolkitContext, _joinableTaskFactory, parameter => DialogResult = true);

            InitializeComponent();

            DataContext = _viewModel;

            ThemeUtil.UpdateDictionariesForTheme(Resources);
        }

        public string LocalPath { get; private set; }

        public Uri RemoteUri { get; private set; }

        public string RepositoryName { get; private set; }

        public new bool Show()
        {
            if (ShowDialog() != true)
            {
                return false;
            }

            var codeCommitSvc = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSCodeCommit)) as IAWSCodeCommit;
            var gitCreds = codeCommitSvc.ObtainGitCredentials(_viewModel.SelectedAccount, _viewModel.SelectedRegion, false);
            if (gitCreds == null)
            {
                return false;
            }

            LocalPath = _viewModel.LocalPath;
            RepositoryName = _viewModel.SelectedRepository.Name;
            RemoteUri = new UriBuilder(_viewModel.SelectedRepository.RepositoryUrl)
            {
                UserName = gitCreds.Username,
                Password = gitCreds.Password
            }.Uri;

            return true;
        }
    }
}
