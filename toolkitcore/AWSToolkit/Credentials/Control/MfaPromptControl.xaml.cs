using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.Credentials.Control
{
    /// <summary>
    /// Interaction logic for MfaPromptControl.xaml
    /// </summary>
    public partial class MfaPromptControl : BaseAWSControl
    {
        private readonly MfaPromptViewModel _viewModel;

        public MfaPromptControl(MfaPromptViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            this.DataContext = _viewModel;
        }

        public override string Title => $"AWS MFA Challenge for {_viewModel.ProfileName}";
    }
}
