using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CodeCatalyst
{
    public interface IAWSCodeCatalyst
    {
        Task<IEnumerable<ICodeCatalystSpace>> GetSpacesAsync(AwsConnectionSettings settings);

        Task<IEnumerable<ICodeCatalystProject>> GetProjectsAsync(string spaceName, AwsConnectionSettings settings);

        Task<IEnumerable<ICodeCatalystRepository>> GetRemoteRepositoriesAsync(string spaceName, string projectName, AwsConnectionSettings settings);

        Task<IEnumerable<ICodeCatalystAccessToken>> GetAccessTokensAsync(AwsConnectionSettings settings);

        Task<ICodeCatalystAccessToken> CreateAccessTokenAsync(string name, DateTime? expiresOn, AwsConnectionSettings settings);
    }
}
