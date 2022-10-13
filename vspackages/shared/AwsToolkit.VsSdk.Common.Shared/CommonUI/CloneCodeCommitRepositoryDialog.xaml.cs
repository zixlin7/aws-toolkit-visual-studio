using Amazon.AWSToolkit;
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
            _viewModel = new CloneCodeCommitRepositoryViewModel(_toolkitContext, _joinableTaskFactory);
            
            InitializeComponent();
            DataContext = _viewModel;

            ThemeUtil.UpdateDictionariesForTheme(Resources);
        }
        public new bool Show()
        {
            var result = ShowDialog() ?? false;

            if (result)
            {
                // TODO put handling to return output details here if necessary
            }

            return result;
        }
    }
}
