using Amazon.AWSToolkit.CodeArtifact.Nodes;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Shared;
using Amazon.CodeArtifact;
using Amazon.CodeArtifact.Model;
using log4net;
using System;
using System.Windows;

namespace Amazon.AWSToolkit.CodeArtifact.Controller
{
    public class GetRepositoryEndpointController : BaseContextCommand
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(GetRepositoryEndpointController));
        private IAWSToolkitShellProvider shellProvider;

        public GetRepositoryEndpointController() : this(ToolkitFactory.Instance.ShellProvider) { }

        public GetRepositoryEndpointController(IAWSToolkitShellProvider shellProvider)
        {
            this.shellProvider = shellProvider;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var repoModel = model as RepoViewModel;
            if (repoModel == null)
                return new ActionResults().WithSuccess(false);
            var domainName = repoModel.Parent.Name;
            var repoName = repoModel.Name;
            return Execute(repoModel.CodeArtifactClient, domainName, repoName);
        }

        public ActionResults Execute(IAmazonCodeArtifact codeArtifactClient, string domainName, string repoName)
        {
            var endpoint  = GenerateURL(codeArtifactClient, domainName, repoName);
            if(string.IsNullOrEmpty(endpoint))
            {
                return new ActionResults().WithSuccess(false);
            }
            try
            {
                Clipboard.SetText(endpoint);     
            }
            catch (Exception e)
            {
                LOGGER.Error("Error copying repository endpoint URL:", e);
                shellProvider.ShowError("Error copying repository endpoint URL: " + e.Message);
                return new ActionResults().WithSuccess(false);
            }
            shellProvider.UpdateStatus(string.Format("Copied NuGet Source to clipboard: {0}/{1}", domainName, repoName));
            return new ActionResults().WithSuccess(true);
        }

        public string GenerateURL(IAmazonCodeArtifact codeArtifactClient, string domainName, string repoName)
        { 
            var request = new GetRepositoryEndpointRequest()
            {
                Domain = domainName,
                Repository = repoName,
            };

            //TODO (sankalbh@): Replace Maven packageFormat with Nuget once AWSSDK.CodeArtifact supports it
            request.Format = PackageFormat.Maven;
            try
            {
                var endpointUrl = codeArtifactClient.GetRepositoryEndpoint(request).RepositoryEndpoint.Replace("maven", "nuget");
                endpointUrl = string.Format("{0}v3/index.json", endpointUrl);
                return endpointUrl;
            }
            catch (Exception e)
            {
                LOGGER.Error(string.Format("Error fetching repository endpoint URL for domain {0}. repository {1}", domainName, repoName), e);
                shellProvider.ShowError("Error fetching repository endpoint URL: " + e.Message);
            }
            return null;
        }
    }
}
