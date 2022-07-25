using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CodeArtifact.CredentialProvider;
using Amazon.AWSToolkit.CodeArtifact.Utils;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Regions;
using Amazon.CodeArtifact;
using Amazon.CodeArtifact.Model;
using Amazon.Runtime;

using log4net;

using NuGet.VisualStudio;

namespace Amazon.AWSToolkit.NuGet
{
    [Export(typeof(IVsCredentialProvider))]

    // Reference for Credential Provider: https://docs.microsoft.com/en-us/nuget/reference/extensibility/nuget-credential-providers-for-visual-studio 
    public class CodeArtifactNuGetCredentialProvider
        : IVsCredentialProvider
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CodeArtifactNuGetCredentialProvider));

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
                Logger.Warn("Provider does not handle acquiring proxy credentials");
                return null;
            }

            // This credential provider does not support a relative Uri.
            if (!uri.IsAbsoluteUri)
            {
                Logger.Warn("Provider does not support relative Uri");
                return null;
            }

            if (!CodeArtifactUri.TryParse(uri, out var codeArtifactUri))
            {
                Logger.Warn("Uri does not match the CodeArtifact Uri scheme");
                return null;
            }

            try
            {
                AWSCredentials credentials = GetAwsCredentials(codeArtifactUri);

                using (var client = new AmazonCodeArtifactClient(credentials,
                           RegionEndpoint.GetBySystemName(codeArtifactUri.Region)))
                {
                    var tokenRequest = new GetAuthorizationTokenRequest()
                    {
                        Domain = codeArtifactUri.Domain, DomainOwner = codeArtifactUri.DomainOwner
                    };

                    var tokenResponse = await client.GetAuthorizationTokenAsync(tokenRequest).ConfigureAwait(false);

                    var response = CreateSuccessResponse(tokenResponse);


                    return response;
                }
            }
            catch (Exception e)
            {
                // One of the cases of this exception might be that user is using wrong profile.
                ToolkitFactory.Instance.ShellProvider.ShowError(
                    string.Format("Failed to get authorization from CodeArtifact: {0}", e.Message));
                Logger.Error("Failed to get CodeArtifact auth token", e);

                //if a user cancel's the process, throw the exception instead of consuming it
                if (e is UserCanceledException)
                {
                    throw;
                }

                return null;
            }
        }

        private AWSCredentials GetAwsCredentials(CodeArtifactUri codeArtifactUri)
        {
            var profileName = DetermineProfileForUri(codeArtifactUri.CodeArtifactEndpoint);
            if (!string.IsNullOrEmpty(profileName))
            {
                var identifier = GetIdentifierForProfile(profileName);
                if (identifier == null)
                {
                    throw new Exception($"Failed to find profile {profileName}");
                }

                var region = RegionEndpoint.GetBySystemName(codeArtifactUri.Region);
                return ToolkitFactory.Instance.CredentialManager.GetAwsCredentials(identifier,
                    new ToolkitRegion
                    {
                        PartitionId = region.PartitionName, Id = region.SystemName, DisplayName = region.DisplayName
                    });
            }

            Logger.Warn("Credentials were null. Using the Fallback credentials");
            return FallbackCredentialsFactory.GetCredentials();
        }

        private CodeArtifactAuthCredentials CreateSuccessResponse(GetAuthorizationTokenResponse tokenResponse)
        {
            var accessToken = tokenResponse.AuthorizationToken;
            var token = new SecureString();

            foreach (char c in accessToken)
            {
                token.AppendChar(c);
            }

            token.MakeReadOnly();
            return new CodeArtifactAuthCredentials("aws", token);
        }

        private string DetermineProfileForUri(Uri requestUri)
        {
            var configuration = Configuration.LoadInstalledConfiguration();

            if (configuration.SourceProfileOverrides != null &&
                configuration.SourceProfileOverrides.TryGetValue(requestUri.AbsoluteUri, out var profile))
            {
                return profile;
            }

            return configuration.DefaultProfile;
        }

        private ICredentialIdentifier GetIdentifierForProfile(string profileName)
        {
            var credManager = ToolkitFactory.Instance.CredentialManager;

            var identifiers = credManager?.GetCredentialIdentifiers().Where(id => id.ProfileName.Equals(profileName));
            return identifiers?.FirstOrDefault();
        }
    }
}
