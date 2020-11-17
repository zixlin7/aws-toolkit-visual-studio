using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeArtifact.CredentialProvider;
using Amazon.AWSToolkit.CodeArtifact.Utils;
using Amazon.AWSToolkit.CodeArtifact.View;
using Amazon.CodeArtifact;
using Amazon.CodeArtifact.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using log4net;
using NuGet.VisualStudio;

namespace Amazon.AWSToolkit.NuGet
{
    [Export(typeof(IVsCredentialProvider))]

    // Reference for Credential Provider: https://docs.microsoft.com/en-us/nuget/reference/extensibility/nuget-credential-providers-for-visual-studio 
    class CodeArtifactNuGetCredentialProvider
    : IVsCredentialProvider
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(CodeArtifactNuGetCredentialProvider));

        public async Task<ICredentials> GetCredentialsAsync(
            Uri uri,
            IWebProxy proxy,
            bool isProxyRequest,
            bool isRetry,
            bool nonInteractive,
            CancellationToken cancellationToken)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            // This credential provider doesn't handle getting proxy credentials.
            if (isProxyRequest)
            {
                LOGGER.Warn("The request is Proxy request");
                return null;
            }

            // This credential provider does not support a relative Uri.
            if (!uri.IsAbsoluteUri)
            {
                LOGGER.Warn("The uri is relative");
                return null;
            }
            if (!CodeArtifactUri.TryParse(uri, out var codeArtifactUri))
            {
                return null;
            }
            try
            {
                AWSCredentials credentials = GetAwsCredentials(codeArtifactUri);

                using (var client = new AmazonCodeArtifactClient(credentials, RegionEndpoint.GetBySystemName(codeArtifactUri.Region)))
                {
                    var tokenRequest = new GetAuthorizationTokenRequest()
                    {
                        Domain = codeArtifactUri.Domain,
                        DomainOwner = codeArtifactUri.DomainOwner
                    };

                    var tokenResponse = await client.GetAuthorizationTokenAsync(tokenRequest).ConfigureAwait(false);

                    var response = CreateSuccessResponse(tokenResponse);


                    return response;
                }
            }
            catch (Exception e)
            {
                // One of the cases of this exception might be that user is using wrong profile.
                LOGGER.Error("Failed to get CodeArtifact auth token", e);
                return null;
            }
        }

        private AWSCredentials GetAwsCredentials(CodeArtifactUri codeArtifactUri)
        {
            var profileName = DetermineProfileForUri(codeArtifactUri.CodeArtifactEndpoint);
            if (!string.IsNullOrEmpty(profileName))
            {
                var account = GetAccountForProfile(profileName);
                if (account == null)
                {
                    throw new Exception($"Failed to find profile {profileName}");
                }
                return account.Credentials;
            }
            LOGGER.Warn("Credentials were null. Using the Fallback credentials");
            return FallbackCredentialsFactory.GetCredentials();
        }

        private CodeArtifactAuthCredentials CreateSuccessResponse(GetAuthorizationTokenResponse tokenResponse)
        {
            var accessToken = tokenResponse.AuthorizationToken;
            var token = new SecureString();

            foreach (char c in accessToken)
                token.AppendChar(c);
            token.MakeReadOnly();
            return new CodeArtifactAuthCredentials("aws", token);

        }

        private string DetermineProfileForUri(Uri requestUri)
        {
            var configuration = Configuration.LoadInstalledConfiguration();

            if (configuration.SourceProfileOverrides != null && configuration.SourceProfileOverrides.TryGetValue(requestUri.AbsoluteUri, out var profile))
            {
                return profile;
            }

            return configuration.DefaultProfile;
        }

        private AccountViewModel GetAccountForProfile(string profileName)
        {
            var rootViewModel = ToolkitFactory.Instance.RootViewModel;
            if (rootViewModel == null)
            {
                return null;
            }
            foreach (var account in rootViewModel.RegisteredAccounts)
            {
                if (account.Profile.Name == profileName)
                {
                    return account;
                }
            }
            return null;
        }
    }
}