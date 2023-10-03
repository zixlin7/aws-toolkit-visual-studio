using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;

using AwsToolkit.VsSdk.Common.CommonUI.Models;

namespace Amazon.AwsToolkit.VsSdk.Common.CommonUI
{
    public partial class SsoLoginDialog : ThemedDialogWindow, ISsoLoginDialog
    {
        private readonly SsoLoginViewModel _viewModel;

        public SsoLoginDialog(ToolkitContext toolkitContext)
        {
            _viewModel = new SsoLoginViewModel(toolkitContext);

            InitializeComponent();
            DataContext = _viewModel;
        }

        public string CredentialName
        {
            get => _viewModel.CredentialName;
            set => _viewModel.CredentialName = value;
        }

        public bool IsBuilderId
        {
            get => _viewModel.IsBuilderId;
            set => _viewModel.IsBuilderId = value;
        }

        public string LoginUri
        {
            get => _viewModel.LoginUri;
            set => _viewModel.LoginUri = value;
        }

        public string UserCode
        {
            get => _viewModel.UserCode;
            set => _viewModel.UserCode = value;
        }

        public new bool Show()
        {
            return ShowModal() == true;
        }
    }
}
