using System.Threading.Tasks;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.Regions;

using CommonUI.Models;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    public partial class VpcSelectionDialog : DialogWindow, IVpcSelectionDialog
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly VpcSelectionViewModel _viewModel;

        private string _vpcId;
        public string VpcId
        {
            get => _vpcId;
            set => _vpcId = value;
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

        public VpcSelectionDialog(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;

            _viewModel = CreateViewModel();

            InitializeComponent();
            DataContext = _viewModel;
        }

        private VpcSelectionViewModel CreateViewModel()
        {
            var vpcRepository = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IVpcRepository)) as IVpcRepository;
            var viewModel = new VpcSelectionViewModel(vpcRepository, _joinableTaskFactory);
            viewModel.OkCommand = new RelayCommand(
                _ => _viewModel.Vpc != null,
                _ => DialogResult = true);

            return viewModel;
        }

        public new bool Show()
        {
            _joinableTaskFactory.Run(async () =>
            {
                await LoadVpcsAsync();
                await SelectAsync(_vpcId);
            });

            var result = ShowModal() ?? false;

            if (result)
            {
                _vpcId = _viewModel.Vpc.Id;
            }

            return result;
        }

        private async Task LoadVpcsAsync()
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            using (var progressDialog = await _toolkitContext.ToolkitHost.CreateProgressDialog())
            {
                progressDialog.Heading1 = "Loading VPCs";
                progressDialog.CanCancel = false;
                progressDialog.Show(1);

                await _viewModel.RefreshVpcsAsync().ConfigureAwait(false);

                await _joinableTaskFactory.SwitchToMainThreadAsync();
                progressDialog.Hide();
            }
        }

        private async Task SelectAsync(string vpcId)
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            _viewModel.Select(vpcId);
        }
    }
}
