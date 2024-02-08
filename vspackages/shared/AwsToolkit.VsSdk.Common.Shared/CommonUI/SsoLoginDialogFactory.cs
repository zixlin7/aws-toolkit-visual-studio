using System.Threading;

using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.Runtime;
using Amazon.Runtime.Credentials.Internal;

using AwsToolkit.VsSdk.Common.CommonUI.Models;

using Microsoft.VisualStudio.Threading;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    public class SsoLoginDialogFactory : ISsoLoginDialogFactory
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly string _credentialName;
        private readonly CancellationToken _cancellationToken;

        public SsoLoginDialogFactory(string credentialName, ToolkitContext toolkitContext,
            JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
        {
            _credentialName = credentialName;
            _cancellationToken = cancellationToken;
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;
        }

        public ISsoLoginDialog CreateSsoCredentialsProviderLoginDialog(SSOAWSCredentials ssoCredentials)
        {
            var viewModel =
                new SsoCredentialsProviderLoginViewModel(ssoCredentials, _toolkitContext, _joinableTaskFactory, _cancellationToken)
                {
                    CredentialName = _credentialName
                };

            return new SsoLoginDialog { DataContext = viewModel };
        }

        public ISsoLoginDialog CreateSsoTokenProviderLoginDialog(ISSOTokenManager ssoTokenManager,
            SSOTokenManagerGetTokenOptions tokenOptions, bool isBuilderId)
        {
            var viewModel = new SsoTokenProviderLoginViewModel(ssoTokenManager, tokenOptions, isBuilderId, _toolkitContext,
                _joinableTaskFactory, _cancellationToken) { CredentialName = _credentialName};
            return new SsoLoginDialog { DataContext = viewModel };
        }
    }
}
