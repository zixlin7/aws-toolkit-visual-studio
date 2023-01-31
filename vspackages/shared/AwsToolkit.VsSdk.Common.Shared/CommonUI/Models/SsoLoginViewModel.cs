using System;
using System.Windows.Forms;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;

using log4net;


namespace AwsToolkit.VsSdk.Common.CommonUI.Models
{
    public class SsoLoginViewModel : BaseModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SsoLoginViewModel));

        private const string _ssoHelpUri =
            "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/sso-credentials.html";

        private const string _builderIdHelpUri =
            "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/codecatalyst-setup.html#codecatalyst-setup-connect";


        private readonly ToolkitContext _toolkitContext;

        public SsoLoginViewModel(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            CancelDialogCommand = new RelayCommand(ExecuteCancelDialog);
            OkCommand = new RelayCommand(ExecuteOk);
            HelpCommand = new RelayCommand(ExecuteHelp);
            CopyCommand = new RelayCommand(ExecuteCopy);
        }

        private bool? _dialogResult;

        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        private bool _isBuilderId;

        public bool IsBuilderId
        {
            get => _isBuilderId;
            set
            {
                SetProperty(ref _isBuilderId, value);
                NotifyPropertyChanged(nameof(HelpUri));
                NotifyPropertyChanged(nameof(DisplayName));
            }
        }

        private string _userCode;

        public string UserCode
        {
            get => _userCode;
            set => SetProperty(ref _userCode, value);
        }

        private string _credentialName;

        public string CredentialName
        {
            get => _credentialName;
            set => SetProperty(ref _credentialName, value);
        }

        private string _loginUri;

        public string LoginUri
        {
            get => _loginUri;
            set => SetProperty(ref _loginUri, value);
        }

        public ICommand CancelDialogCommand { get; }

        public ICommand OkCommand { get; }

        public ICommand HelpCommand { get; }

        public ICommand CopyCommand { get; }

        public string DisplayName => IsBuilderId ? "AWS Builder ID" : "AWS IAM Identity Center";

        public string HelpUri => IsBuilderId ? _builderIdHelpUri : _ssoHelpUri;

        private void ExecuteHelp(object obj)
        {
            _toolkitContext.ToolkitHost.OpenInBrowser(HelpUri, false);
        }

        private void ExecuteOk(object obj)
        {
            ExecuteCopy(obj);
            _toolkitContext.ToolkitHost.OpenInBrowser(LoginUri, false);
            DialogResult = true;
        }

        private void ExecuteCopy(object obj)
        {
            try
            {
                var content = (string) obj;
                Clipboard.SetText(content);
            }
            catch (Exception e)
            {
                _logger.Error($"Error copying content to clipboard", e);
                _toolkitContext.ToolkitHost.OutputToHostConsole(
                    $"Error copying content to clipboard: {e.Message}");
            }
        }

        private void ExecuteCancelDialog(object obj)
        {
            DialogResult = false;
        }
    }
}
