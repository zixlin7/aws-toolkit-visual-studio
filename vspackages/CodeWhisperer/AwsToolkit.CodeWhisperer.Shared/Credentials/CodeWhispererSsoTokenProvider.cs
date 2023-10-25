using System;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    public class CodeWhispererSsoTokenProvider : ICodeWhispererSsoTokenProvider
    {
        private readonly IToolkitContextProvider _toolkitContextProvider;

        public CodeWhispererSsoTokenProvider(IToolkitContextProvider toolkitContextProvider)
        {
            _toolkitContextProvider = toolkitContextProvider;
        }

#pragma warning disable IDE0046 // Convert to conditional expression (conditional expression not as legible)
        public bool TrySilentGetSsoToken(ICredentialIdentifier credentialId, ToolkitRegion ssoRegion, out AWSToken token)
        {
            token = null;

            try
            {
                var toolkitContext = _toolkitContextProvider.GetToolkitContext();

                // Try to refresh the token in a way that fails if the user would need to be prompted for a SSO Login
                if (!credentialId.HasValidToken(SonoCredentialProviderFactory.CodeWhispererSsoSession, toolkitContext.ToolkitHost))
                {
                    return false;
                }

                // The token has been refreshed. We can now obtain it through the credentials engine
                // with confidence that the user will not get prompted to perform an SSO login.
                return TryGetSsoToken(credentialId, ssoRegion, out token);
            }
            catch (Exception)
            {
                return false;
            }
        }
#pragma warning restore IDE0046 // Convert to conditional expression

        public bool TryGetSsoToken(ICredentialIdentifier credentialId, ToolkitRegion ssoRegion, out AWSToken token)
        {
            token = null;

            try
            {
                return _toolkitContextProvider.GetToolkitContext()
                    .CredentialManager.GetToolkitCredentials(credentialId, ssoRegion)
                    .GetTokenProvider().TryResolveToken(out token);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
