using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Urls;
using Amazon.AWSToolkit.Util;
using Amazon.CodeCatalyst;
using Amazon.CodeCatalyst.Model;

using log4net;

namespace Amazon.AWSToolkit.CodeCatalyst
{
    internal class CodeCatalystPluginService : IAWSCodeCatalyst
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CodeCatalystPluginService));

        private static readonly string _serviceName = ServiceNames.CodeCatalyst;

        private readonly ToolkitContext _toolkitContext;

        internal CodeCatalystPluginService(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public async Task<IEnumerable<ICodeCatalystSpace>> GetSpacesAsync(AwsConnectionSettings settings, CancellationToken cancellationToken = default)
        {
            Arg.NotNull(settings, nameof(settings));

            var client = GetCodeCatalystClient(settings);
            var spaces = new List<ICodeCatalystSpace>();
            var res = new ListSpacesResponse();

            do
            {
                var req = new ListSpacesRequest() { NextToken = res.NextToken };
                res = await client.ListSpacesAsync(req, cancellationToken);

                spaces.AddAll(res.Items.Select(space => new CodeCatalystSpace(space)));

            } while (!string.IsNullOrWhiteSpace(res.NextToken));

            return spaces;
        }

        public async Task<IEnumerable<ICodeCatalystProject>> GetProjectsAsync(string spaceName, AwsConnectionSettings settings, CancellationToken cancellationToken = default)
        {
            Arg.NotNull(settings, nameof(settings));
            Arg.NotNullOrWhitespace(spaceName, nameof(spaceName));

            var client = GetCodeCatalystClient(settings);
            var projects = new List<ICodeCatalystProject>();
            var res = new ListProjectsResponse();

            do
            {
                var req = new ListProjectsRequest()
                {
                    SpaceName = spaceName,
                    NextToken = res.NextToken
                };
                res = await client.ListProjectsAsync(req, cancellationToken);

                projects.AddAll(res.Items.Select(project => new CodeCatalystProject(spaceName, project)));

            } while (!string.IsNullOrWhiteSpace(res.NextToken));

            return projects;
        }

        public async Task<IEnumerable<ICodeCatalystRepository>> GetRemoteRepositoriesAsync(string spaceName, string projectName, AwsConnectionSettings settings, CancellationToken cancellationToken = default)
        {
            Arg.NotNull(settings, nameof(settings));
            Arg.NotNullOrWhitespace(spaceName, nameof(spaceName));
            Arg.NotNullOrWhitespace(projectName, nameof(projectName));

            var client = GetCodeCatalystClient(settings);
            var repos = new List<ICodeCatalystRepository>();
            var res = new ListSourceRepositoriesResponse();
            CloneUrlsFactoryAsync factory = (string repoName) => GetCloneUrlsAsync(spaceName, projectName, repoName, settings);

            do
            {
                var req = new ListSourceRepositoriesRequest()
                {
                    SpaceName = spaceName,
                    ProjectName = projectName,
                    NextToken = res.NextToken
                };
                res = await client.ListSourceRepositoriesAsync(req, cancellationToken);

                repos.AddAll(res.Items.Select(repo => new CodeCatalystRepository(factory, spaceName, projectName, repo)));

            } while (!string.IsNullOrWhiteSpace(res.NextToken));

            return repos;
        }

        public Task<IEnumerable<ICodeCatalystAccessToken>> GetAccessTokensAsync(AwsConnectionSettings settings, CancellationToken cancellationToken = default)
        {
            Arg.NotNull(settings, nameof(settings));

            var store = ServiceSpecificCredentialStore.Instance;

            return Task.FromResult(
                store.TryGetCredentialsForService(CodeCatalystAccessToken._defaultAccountArtifactsId, _serviceName, out ServiceSpecificCredentials creds) ?
                new List<ICodeCatalystAccessToken>() { new CodeCatalystAccessToken(creds) } :
                Enumerable.Empty<ICodeCatalystAccessToken>());
        }

        public async Task<ICodeCatalystAccessToken> CreateAccessTokenAsync(string name, DateTime? expiresOn, AwsConnectionSettings settings, CancellationToken cancellationToken = default)
        {
            Arg.NotNull(settings, nameof(settings));

            var store = ServiceSpecificCredentialStore.Instance;

            using (var client = GetCodeCatalystClient(settings))
            {
                var req = new CreateAccessTokenRequest() { Name = name };
                if (expiresOn.HasValue)
                {
                    req.ExpiresTime = expiresOn.Value;
                }
                var res = await client.CreateAccessTokenAsync(req, cancellationToken);

                // TODO Remove assignment of name to response in line below once P74543357 has been resolved.
                res.Name = name;
                var token = new CodeCatalystAccessToken(res);

                store.SaveCredentialsForService(CodeCatalystAccessToken._defaultAccountArtifactsId, _serviceName, token.Name, token.Secret, res.ExpiresTime);

                return token;
            }
        }

        private async Task<CloneUrls> GetCloneUrlsAsync(string spaceName, string projectName, string repoName, AwsConnectionSettings settings)
        {
            var client = GetCodeCatalystClient(settings);

            var req = new GetSourceRepositoryCloneUrlsRequest()
            {
                SpaceName = spaceName,
                ProjectName = projectName,
                SourceRepositoryName = repoName
            };

            var res = await client.GetSourceRepositoryCloneUrlsAsync(req);

            return new CloneUrls(new Uri(res.Https));
        }

        private IAmazonCodeCatalyst GetCodeCatalystClient(AwsConnectionSettings settings)
        {
            var creds = _toolkitContext.CredentialManager.GetToolkitCredentials(settings);

            var config = new AmazonCodeCatalystConfig()
            {
                ServiceURL = ServiceUrls.CodeCatalyst,
                AWSTokenProvider = creds.GetTokenProvider(),
            };

            return _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonCodeCatalystClient>(settings, config);
        }
    }
}
