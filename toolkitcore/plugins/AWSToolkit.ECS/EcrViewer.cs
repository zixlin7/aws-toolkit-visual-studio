using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Ecr;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.Regions;
using Amazon.ECR;
using Amazon.ECR.Model;

namespace Amazon.AWSToolkit.ECS
{
    public class EcrViewer : IEcrViewer
    {
        private readonly ToolkitContext _toolkitContext;

        public EcrViewer(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public void ViewRepository(string repoName, AwsConnectionSettings connectionSettings)
        {
            if (string.IsNullOrWhiteSpace(repoName))
            {
                throw new ArgumentException("Repo Name cannot be null or empty.");
            }

            IAmazonECR ecrClient = CreateEcrClient(connectionSettings);

            var model = CreateRepositoryModel(repoName, ecrClient);
            var viewController = new ViewRepositoryController(model, connectionSettings, _toolkitContext, ecrClient);

            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                viewController.Execute();
            });
        }

        private IAmazonECR CreateEcrClient(AwsConnectionSettings connectionSettings)
        {
            return _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonECRClient>(connectionSettings.CredentialIdentifier, connectionSettings.Region);
        }

        private ViewRepositoryModel CreateRepositoryModel(string repoName, IAmazonECR ecrClient)
        {
            return new ViewRepositoryModel() { Repository = LoadRepository(repoName, ecrClient), };
        }

        private RepositoryWrapper LoadRepository(string repoName, IAmazonECR ecrClient)
        {
            var request = new DescribeRepositoriesRequest { RepositoryNames = new List<string> { repoName } };

            var response = ecrClient.DescribeRepositories(request);
            if (response.Repositories.Count != 1)
            {
                throw new Exception($"Failed to find repository: {repoName}");
            }

            return new RepositoryWrapper(response.Repositories.First());
        }
    }
}
