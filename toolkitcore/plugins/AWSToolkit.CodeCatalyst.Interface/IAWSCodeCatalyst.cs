using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CodeCatalyst
{
    public interface IAWSCodeCatalyst
    {
        Task<IEnumerable<ICodeCatalystSpace>> GetSpacesAsync(AwsConnectionSettings settings, CancellationToken cancellationToken = default);

        Task<IEnumerable<ICodeCatalystProject>> GetProjectsAsync(string spaceName, AwsConnectionSettings settings, CancellationToken cancellationToken = default);

        Task<IEnumerable<ICodeCatalystRepository>> GetRemoteRepositoriesAsync(string spaceName, string projectName, AwsConnectionSettings settings, CancellationToken cancellationToken = default);

        Task<IEnumerable<ICodeCatalystAccessToken>> GetAccessTokensAsync(AwsConnectionSettings settings, CancellationToken cancellationToken = default);

        Task<ICodeCatalystAccessToken> CreateAccessTokenAsync(string name, DateTime? expiresOn, AwsConnectionSettings settings, CancellationToken cancellationToken = default);

        Task<string> GetUserNameAsync(string userId, AwsConnectionSettings settings);
    }

    public static class IAWSCodeCatalystExtensionMethods
    {
        public static Task<ICodeCatalystAccessToken> CreateDefaultAccessTokenAsync(this IAWSCodeCatalyst @this, DateTime? expiresOn, AwsConnectionSettings settings, CancellationToken cancellationToken = default)
        {
            // While this could've been achieved with a default value for the name parameter in CreateAccessTokenAsync, it would make the primary (and thus first)
            // parameter optional which would make for more verbose call sites as the other parameter names would have to be supplied.  Moving name to the end of
            // the parameter list would be inconsistent with the other methods defined in IAWSCodeCatalyst.

            return @this.CreateAccessTokenAsync("aws-toolkits-vs-token", expiresOn, settings, cancellationToken);
        }
    }
}
