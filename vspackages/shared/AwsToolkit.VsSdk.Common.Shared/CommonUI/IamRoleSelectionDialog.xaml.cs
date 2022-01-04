using System.Threading.Tasks;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.IdentityManagement;
using Amazon.AWSToolkit.Regions;

using CommonUI.Models;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    public partial class IamRoleSelectionDialog : DialogWindow, IIamRoleSelectionDialog
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IamRoleSelectionViewModel _viewModel;

        public string RoleArn
        {
            get => _viewModel.RoleArn;
            set => _viewModel.RoleArn = value;
        }

        public ICredentialIdentifier CredentialsId
        {
            get => _viewModel.CredentialsId;
            set => _viewModel.CredentialsId = value;
        }

        public ToolkitRegion Region
        {
            get => _viewModel.Region;
            set => _viewModel.Region = value;
        }

        public IamRoleSelectionDialog(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;

            _viewModel = CreateViewModel();

            InitializeComponent();
            DataContext = _viewModel;
        }

        private IamRoleSelectionViewModel CreateViewModel()
        {
            var iamEntities = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IIamEntityRepository)) as IIamEntityRepository;
            var viewModel = new IamRoleSelectionViewModel(iamEntities, _joinableTaskFactory);
            viewModel.OkCommand = new RelayCommand(
                _ => !string.IsNullOrWhiteSpace(_viewModel.RoleArn),
                _ => DialogResult = true);

            return viewModel;
        }

        public new bool Show()
        {
            _joinableTaskFactory.Run(async () =>
            {
                await LoadRolesAsync();
            });

            return ShowModal() ?? false;
        }

        private async Task LoadRolesAsync()
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            using (var progressDialog = await _toolkitContext.ToolkitHost.CreateProgressDialog())
            {
                progressDialog.Heading1 = "Loading IAM Roles";
                progressDialog.CanCancel = false;
                progressDialog.Show(1);

                await _viewModel.RefreshRolesAsync().ConfigureAwait(false);

                await _joinableTaskFactory.SwitchToMainThreadAsync();
                progressDialog.Hide();
            }
        }
    }
}
