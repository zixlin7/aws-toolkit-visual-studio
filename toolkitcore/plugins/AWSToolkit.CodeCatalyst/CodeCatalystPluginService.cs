using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Urls;
using Amazon.CodeCatalyst;
using Amazon.CodeCatalyst.Model;

using log4net;

namespace Amazon.AWSToolkit.CodeCatalyst
{
    internal class CodeCatalystPluginService : IAWSCodeCatalyst
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CodeCatalystPluginService));

        private readonly ToolkitContext _toolkitContext;

        internal CodeCatalystPluginService(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public async Task<IEnumerable<ICodeCatalystSpace>> GetSpacesAsync(AwsConnectionSettings settings)
        {
            Arg.NotNull(settings, nameof(settings));

            var client = GetCodeCatalystClient(settings);
            var spaces = new List<ICodeCatalystSpace>();
            var res = new ListOrganizationsResponse();

            do
            {
                var req = new ListOrganizationsRequest() { NextToken = res.NextToken };
                res = await client.ListOrganizationsAsync(req);

                spaces.AddAll(res.Items.Select(space => new CodeCatalystSpace(space)));

            } while (!string.IsNullOrWhiteSpace(res.NextToken));

            return spaces;
        }

        public async Task<IEnumerable<ICodeCatalystProject>> GetProjectsAsync(string spaceName, AwsConnectionSettings settings)
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
                    OrganizationName = spaceName,
                    NextToken = res.NextToken
                };
                res = await client.ListProjectsAsync(req);

                projects.AddAll(res.Items.Select(project => new CodeCatalystProject(spaceName, project)));

            } while (!string.IsNullOrWhiteSpace(res.NextToken));

            return projects;
        }

        public async Task<IEnumerable<ICodeCatalystRepository>> GetRemoteRepositoriesAsync(string spaceName, string projectName, AwsConnectionSettings settings)
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
                    OrganizationName = spaceName,
                    ProjectName = projectName,
                    NextToken = res.NextToken
                };
                res = await client.ListSourceRepositoriesAsync(req);

                repos.AddAll(res.Items.Select(repo => new CodeCatalystRepository(factory, spaceName, projectName, repo)));

            } while (!string.IsNullOrWhiteSpace(res.NextToken));

            return repos;
        }

        public Task<IEnumerable<ICodeCatalystAccessToken>> GetAccessTokensAsync(AwsConnectionSettings settings)
        {
            Arg.NotNull(settings, nameof(settings));

            // TODO IDE-8983
            //var client = GetCodeCatalystClient(settings);

            // Write a class(es) around getting access token from ServiceSpecificCredentialStore and fetching a new one after expiration
            //var patReq = new CreateAccessTokenRequest()
            //{
            //    Name = "aws-toolkits-vs-token"
            //};
            //var patRes = await client.CreateAccessTokenAsync(patReq);

            return Task.FromResult((new List<ICodeCatalystAccessToken>() { new CodeCatalystAccessToken("awsId:default", "replace me with a PAT for testing until IDE-8983", DateTime.Now) }).AsEnumerable());
        }

        private async Task<CloneUrls> GetCloneUrlsAsync(string spaceName, string projectName, string repoName, AwsConnectionSettings settings)
        {
            var client = GetCodeCatalystClient(settings);

            var req = new GetSourceRepositoryCloneUrlsRequest()
            {
                OrganizationName = spaceName,
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
