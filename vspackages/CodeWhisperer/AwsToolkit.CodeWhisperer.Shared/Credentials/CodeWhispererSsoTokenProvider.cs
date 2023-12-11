using System;

using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Exceptions;
using Amazon.Runtime;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    public class CodeWhispererSsoTokenProvider : ICodeWhispererSsoTokenProvider
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CodeWhispererSsoTokenProvider));
        private readonly IToolkitContextProvider _toolkitContextProvider;

        public CodeWhispererSsoTokenProvider(IToolkitContextProvider toolkitContextProvider)
        {
            _toolkitContextProvider = toolkitContextProvider;
        }

#pragma warning disable IDE0046 // Convert to conditional expression (conditional expression not as legible)
        public bool TrySilentGetSsoToken(ConnectionProperties connectionProperties, out AWSToken token)
        {
            token = null;

            try
            {
                // Try to refresh the token in a way that fails if the user would need to be prompted for a SSO Login
                if (!connectionProperties.CredentialIdentifier
                        .HasValidCodeWhispererConnection(_toolkitContextProvider.GetToolkitContext()))
                {
                    return false;
                }

                // The token has been refreshed. We can now obtain it through the credentials engine
                // with confidence that the user will not get prompted to perform an SSO login.
                return TryGetSsoToken(connectionProperties, out token) == TaskStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }
#pragma warning restore IDE0046 // Convert to conditional expression

        public TaskStatus TryGetSsoToken(ConnectionProperties connectionProperties, out AWSToken token)
        {
            token = null;

            try
            {
                return _toolkitContextProvider.GetToolkitContext()
                    .CredentialManager.GetToolkitCredentials(connectionProperties.CredentialIdentifier, connectionProperties.SsoRegion)
                    .GetTokenProvider().TryResolveToken(out token)
                    ? TaskStatus.Success
                    : TaskStatus.Fail;
            }
            catch (UserCanceledException)
            {
                return TaskStatus.Cancel;
            }
            catch (Exception e)
            {
                _logger.Error($"Failure resolving SSO Token for {connectionProperties.CredentialIdentifier?.Id}", e);
                return TaskStatus.Fail;
            }
        }
    }
}
