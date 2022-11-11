﻿using System;
using System.Linq;
using System.Windows.Input;

using Amazon.AWSToolkit.CodeCatalyst;
using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;

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

        public CloneCodeCatalystRepositoryDialog(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;
            _viewModel = new CloneCodeCatalystRepositoryViewModel(_toolkitContext, _joinableTaskFactory);
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

            DataContext = _viewModel;
        }

        public AwsConnectionSettings ConnectionSettings { get; private set; }

        public string RepositoryName { get; private set; }

        public Uri CloneUrl { get; private set; }

        public string LocalPath { get; private set; }

        public new bool Show()
        {
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
    }
}
