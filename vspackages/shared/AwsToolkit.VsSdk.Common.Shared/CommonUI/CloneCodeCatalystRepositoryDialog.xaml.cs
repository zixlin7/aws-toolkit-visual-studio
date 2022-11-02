using System;
using System.Linq;
using System.Windows.Input;

using Amazon.AWSToolkit.CodeCatalyst;
using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;

using CommonUI.Models;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    public partial class CloneCodeCatalystRepositoryDialog : DialogWindow, ICloneCodeCatalystRepositoryDialog
    {
        private const string _defaultName = "aws-toolkits-vs-token";

        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly CloneCodeCatalystRepositoryViewModel _viewModel;

        public CloneCodeCatalystRepositoryDialog(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;
            _viewModel = new CloneCodeCatalystRepositoryViewModel(_toolkitContext, _joinableTaskFactory, parameter => DialogResult = true);

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

        public string RepositoryName { get; private set; }

        public Uri CloneUrl { get; private set; }

        public string LocalPath { get; private set; }

        public new bool Show()
        {
            if (ShowModal() != true)
            {
                return false;
            }

            var codeCatalyst = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSCodeCatalyst)) as IAWSCodeCatalyst;

            _joinableTaskFactory.Run(async () =>
            {
                var pat = (await codeCatalyst.GetAccessTokensAsync(_viewModel.ConnectionSettings)).FirstOrDefault() ??
                          (await codeCatalyst.CreateAccessTokenAsync(_defaultName, null, _viewModel.ConnectionSettings));

                CloneUrl = await _viewModel.SelectedRepository.GetCloneUrlAsync(CloneUrlType.Https, pat);
            });

            RepositoryName = _viewModel.SelectedRepository.Name;
            LocalPath = _viewModel.LocalPath;

            return true;
        }
    }
}
