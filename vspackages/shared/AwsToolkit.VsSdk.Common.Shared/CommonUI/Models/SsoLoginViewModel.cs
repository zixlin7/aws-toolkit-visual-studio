using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Tasks;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime;
using Amazon.Runtime.Credentials.Internal;

using log4net;

using Microsoft.VisualStudio.Threading;

using CredentialType = Amazon.AwsToolkit.Telemetry.Events.Generated.CredentialType;
using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace AwsToolkit.VsSdk.Common.CommonUI.Models
{
    public class SsoLoginViewModel : BaseModel, IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SsoLoginViewModel));

        private readonly JoinableTaskFactory _joinableTaskFactory;
        private const string _ssoHelpUri =
            "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/sso-credentials.html";

        private const string _builderIdHelpUri =
            "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/builder-id.html";

        private readonly CancellationTokenSource _linkedCancellationTokenSource;

        protected ToolkitContext _toolkitContext { get; }

        public SsoLoginViewModel(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory,
            CancellationToken cancellationToken)
        {
            _joinableTaskFactory = joinableTaskFactory;
            _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _toolkitContext = toolkitContext;
            CancelDialogCommand = new RelayCommand(ExecuteCancelDialog);
            BeginLoginFlowCommand = new RelayCommand(ExecuteBeginLoginFlow);
            HelpCommand = new RelayCommand(ExecuteHelp);
        }

        /// <summary>
        /// Represents the linked cancellation token combining the dialog and caller's tokens(specified with the constructor)
        /// </summary>
        protected CancellationToken _linkedCancellationToken => _linkedCancellationTokenSource.Token;


        private bool? _dialogResult;

        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        private bool _isBuilderId;
        /// <summary>
        /// Indicates whether the connection represents an AWS SSO Credential profile
        /// or an AWS Builder ID connection
        /// </summary>
        public bool IsBuilderId
        {
            get => _isBuilderId;
            protected set
            {
                SetProperty(ref _isBuilderId, value);
                NotifyPropertyChanged(nameof(HelpUri));
                NotifyPropertyChanged(nameof(DisplayName));
            }
        }

        private string _userCode;

        /// <summary>
        /// User code required for authorization
        /// </summary>
        public string UserCode
        {
            get => _userCode;
            set => SetProperty(ref _userCode, value);
        }

        private string _loginUri;
        /// <summary>
        /// Login Uri to be opened in browser
        /// </summary>
        public string LoginUri
        {
            get => _loginUri;
            set => SetProperty(ref _loginUri, value);
        }

        private bool _inProgress;
        /// <summary>
        /// Indicates whether the login process is in progress in the browser
        /// </summary>
        public bool InProgress
        {
            get => _inProgress;
            private set => SetProperty(ref _inProgress, value);
        }

        private TaskResult _loginResult = new TaskResult() { Status = TaskStatus.Cancel };
        /// <summary>
        /// Represents result of the login process
        /// </summary>
        public TaskResult LoginResult
        {
            get => _loginResult;
            set => SetProperty(ref _loginResult, value);
        }

        private string _credentialName;
        /// <summary>
        /// The name of the credential profile for which connection is being made
        /// only applicable for SSO based credential profile
        /// </summary>
        public string CredentialName
        {
            get => _credentialName;
            set => SetProperty(ref _credentialName, value);
        }

        public ICommand CancelDialogCommand { get; }

        public ICommand BeginLoginFlowCommand { get; }

        public ICommand HelpCommand { get; }

        public string DisplayName => IsBuilderId ? "AWS Builder ID" : "AWS IAM Identity Center";

        public string HelpUri => IsBuilderId ? _builderIdHelpUri : _ssoHelpUri;

        public SsoToken SsoToken { get; set; }

        public ImmutableCredentials Credentials { get; set; }

        public void RecordLoginMetric()
        {
            var data = LoginResult.CreateMetricData<AwsLoginWithBrowser>();
            data.Result = LoginResult.Status.AsTelemetryResult();
            data.CredentialType = IsBuilderId ? CredentialType.BearerToken : CredentialType.SsoProfile;

            _toolkitContext.TelemetryLogger.RecordAwsLoginWithBrowser(data);
        }

        public void BeginLoginFlow()
        {
            _joinableTaskFactory.RunAsync(async () =>
            {
                await Task.Yield();
                await TaskScheduler.Default;
                await BeginLoginFlowAsync();

            }).Task.LogExceptionAndForget();
        }

        protected async Task BeginLoginFlowAsync()
        {
            try
            {
                _toolkitContext.ToolkitHost.OutputToHostConsole($"{DisplayName} Log in flow started for Credentials: {CredentialName}", false);
                await ExecuteLoginFlowAsync();
                LoginResult.Status = TaskStatus.Success;
                DialogResult = true;
            }
            catch (Exception e)
            {
                // If dialog is already closed, silently swallow this
                // Note: This check has been added as a safeguard in scenarios when the AWS SDK calls returns control to this dialog after the user has already closed/cancelled the login process
                // As of writing this(Jan 24), the AWS SDK does not provide a way to inject and handle cancellation tokens to calls like GetToken and GetCredentials.
                // So when a user cancels the process in the dialog, the browser process still continues to happen async and the SDK continues to poll for the token until timeout occurs
                if (DialogResult != null)
                {
                    return;
                }

                LoginResult.Status = TaskStatus.Fail;
                LoginResult.Exception = e;
                _logger.Error($"Log in failed for {DisplayName} based Credentials {CredentialName}: {e.Message}", e);
                DialogResult = false;
                throw;
            }
        }

        /// <summary>
        /// Populates the dialog with required sso verification parameters that become available when a user login is required as part of the SSO login process
        /// </summary>
        /// <param name="ssoVerification"></param>
        protected void OnSsoVerificationCallback(SsoVerificationArguments ssoVerification)
        {
            UserCode = ssoVerification.UserCode;
            LoginUri = ssoVerification.VerificationUriComplete;
        }

        protected virtual Task ExecuteLoginFlowAsync()
        {
            return Task.CompletedTask;
        }

        private void ExecuteHelp(object obj)
        {
            _toolkitContext.ToolkitHost.OpenInBrowser(HelpUri, false);
        }

        private void ExecuteBeginLoginFlow(object obj)
        {
            _toolkitContext.ToolkitHost.OpenInBrowser(LoginUri, false);
            InProgress = true;
        }

        private void ExecuteCancelDialog(object obj)
        {
            _linkedCancellationTokenSource.Cancel();
            // Setting the dialog result to false may not be required if SDK adds support for cancellation token which would throw appropriate exceptions and stop the process
            DialogResult = false;
            LoginResult.Status = TaskStatus.Cancel;
        }

        public void Dispose()
        {
            _linkedCancellationTokenSource?.Dispose();
        }
    }

    public class SsoTokenProviderLoginViewModel : SsoLoginViewModel
    {
        private readonly ISSOTokenManager _ssoTokenManager;
        private readonly SSOTokenManagerGetTokenOptions _tokenManagerOptions;

        public SsoTokenProviderLoginViewModel(ISSOTokenManager ssoTokenManager,
            SSOTokenManagerGetTokenOptions tokenManagerOptions, bool isBuilderId, ToolkitContext toolkitContext,
            JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken) : base(toolkitContext,
            joinableTaskFactory, cancellationToken)
        {
            _ssoTokenManager = ssoTokenManager;
            _tokenManagerOptions = tokenManagerOptions;
            IsBuilderId = isBuilderId;
        }

        protected override async Task ExecuteLoginFlowAsync()
        {
            SsoToken = await GetTokenAsync().ConfigureAwait(false);
        }

        private async Task<SsoToken> GetTokenAsync()
        {
            _tokenManagerOptions.SsoVerificationCallback = OnSsoVerificationCallback;
            return await _ssoTokenManager.GetTokenAsync(_tokenManagerOptions, _linkedCancellationToken).ConfigureAwait(false);
        }
    }

    public class SsoCredentialsProviderLoginViewModel : SsoLoginViewModel
    {
        private readonly SSOAWSCredentials _ssoCredentials;


        public SsoCredentialsProviderLoginViewModel(SSOAWSCredentials credentials, ToolkitContext toolkitContext,
            JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken) : base(toolkitContext,
            joinableTaskFactory, cancellationToken)
        {
            _ssoCredentials = credentials;
            IsBuilderId = false;
        }

        protected override async Task ExecuteLoginFlowAsync()
        {
            Credentials = await GetCredentialsAsync().ConfigureAwait(false);
        }

        private async Task<ImmutableCredentials> GetCredentialsAsync()
        {
            _ssoCredentials.Options.SsoVerificationCallback = OnSsoVerificationCallback;
            return await _ssoCredentials.GetCredentialsAsync().ConfigureAwait(false);
        }
    }
}
