using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CodeArtifact.CredentialProvider;
using Amazon.AWSToolkit.CodeArtifact.Utils;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tasks;
using Amazon.CodeArtifact;
using Amazon.CodeArtifact.Model;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

using log4net;

using Microsoft.VisualStudio.Threading;

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

            return await GetCredentialsAsync(codeArtifactUri, cancellationToken);
        }

        private async Task<ICredentials> GetCredentialsAsync(CodeArtifactUri codeArtifactUri, CancellationToken cancellationToken)
        {
            AWSCredentials credentials = null;
            RegionEndpoint regionEndpoint = null;

            try
            {
                regionEndpoint = RegionEndpoint.GetBySystemName(codeArtifactUri.Region);
                credentials = GetAwsCredentials(codeArtifactUri, regionEndpoint);

                using (var client = new AmazonCodeArtifactClient(credentials, regionEndpoint))
                {
                    var tokenRequest = new GetAuthorizationTokenRequest()
                    {
                        Domain = codeArtifactUri.Domain, DomainOwner = codeArtifactUri.DomainOwner
                    };

                    var tokenResponse = await client.GetAuthorizationTokenAsync(tokenRequest, cancellationToken)
                        .ConfigureAwait(false);

                    // Record success, but don't block VS from getting requested credentials
                    RecordCredentialsRequestSuccessAsync(credentials, regionEndpoint).LogExceptionAndForget();

                    return CreateSuccessResponse(tokenResponse);
                }
            }
            catch (Exception e)
            {
                // Record failure, but don't block the VS request from completing
                RecordCredentialsRequestFailureAsync(credentials, regionEndpoint, e).LogExceptionAndForget();

                // One of the cases of this exception might be that user is using wrong profile.
                ToolkitFactory.Instance.ShellProvider.ShowError(
                    $"Failed to get authorization from CodeArtifact: {e.Message}");
                Logger.Error("Failed to get CodeArtifact auth token", e);

                // if a user cancels the process, throw the exception instead of consuming it
                if (e is UserCanceledException)
                {
                    throw;
                }

                return null;
            }
        }

        private AWSCredentials GetAwsCredentials(CodeArtifactUri codeArtifactUri, RegionEndpoint regionEndpoint)
        {
            var profileName = DetermineProfileForUri(codeArtifactUri.CodeArtifactEndpoint);
            if (!string.IsNullOrEmpty(profileName))
            {
                var identifier = GetIdentifierForProfile(profileName);
                if (identifier == null)
                {
                    throw new Exception($"Failed to find profile {profileName}");
                }

                return ToolkitFactory.Instance.CredentialManager.GetAwsCredentials(identifier,
                    new ToolkitRegion
                    {
                        PartitionId = regionEndpoint.PartitionName,
                        Id = regionEndpoint.SystemName,
                        DisplayName = regionEndpoint.DisplayName
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

            var identifiers = credManager?.GetCredentialIdentifiers()
                .Where(id => (credManager?.Supports(id, AwsConnectionType.AwsCredentials) ?? false)
                             && id.ProfileName.Equals(profileName));
            return identifiers?.FirstOrDefault();
        }

        private async Task RecordCredentialsRequestSuccessAsync(AWSCredentials awsCredentials, RegionEndpoint region)
        {
            await RecordCredentialsRequestAsync(awsCredentials, region, Result.Succeeded);
        }

        private async Task RecordCredentialsRequestFailureAsync(AWSCredentials awsCredentials, RegionEndpoint region,
            Exception exception)
        {
            if (exception is UserCanceledException || exception is OperationCanceledException)
            {
                await RecordCredentialsRequestAsync(awsCredentials, region, Result.Cancelled);
            }
            else
            {
                string reason = "unknown";

                if (exception is AmazonServiceException serviceException)
                {
                    reason = serviceException.ErrorCode;
                }

                await RecordCredentialsRequestAsync(awsCredentials, region, Result.Failed, reason);
            }
        }

        private async Task RecordCredentialsRequestAsync(AWSCredentials awsCredentials, RegionEndpoint region,
            Result result, string reason = null)
        {
            await TaskScheduler.Default;

            var accountId = await GetAccountIdAsync(awsCredentials, region) ?? MetadataValue.NotSet;

            ToolkitFactory.Instance.TelemetryLogger.RecordCodeartifactCredentialsRequest(new CodeartifactCredentialsRequest()
            {
                AwsAccount = accountId,
                AwsRegion = region?.SystemName ?? MetadataValue.NotSet,
                Result = result,
                Reason = reason,
            });
        }

        private async Task<string> GetAccountIdAsync(AWSCredentials awsCredentials, RegionEndpoint regionEndpoint)
        {
            try
            {
                var stsClient = new AmazonSecurityTokenServiceClient(awsCredentials, regionEndpoint);

                var response = await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());
                return response.Account;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to get account Id, emitting metrics with no account", e);

                return null;
            }
        }
    }
}
