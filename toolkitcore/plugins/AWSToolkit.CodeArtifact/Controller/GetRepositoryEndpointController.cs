using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CodeArtifact.Nodes;
using Amazon.AWSToolkit.CodeArtifact.Utils;
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
        private readonly ITelemetryLogger _telemetryLogger;

        public GetRepositoryEndpointController() : this(ToolkitFactory.Instance.ShellProvider, ToolkitFactory.Instance.TelemetryLogger) { }

        public GetRepositoryEndpointController(IAWSToolkitShellProvider shellProvider, ITelemetryLogger telemetryLogger)
        {
            this.shellProvider = shellProvider;
            this._telemetryLogger = telemetryLogger;
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
                _telemetryLogger.RecordCodeartifactGetRepoUrl(new CodeartifactGetRepoUrl()
                {
                    Result = Result.Failed,
                    CodeartifactPackageType = "nuget"
                });
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
                _telemetryLogger.RecordCodeartifactGetRepoUrl(new CodeartifactGetRepoUrl()
                {
                    Result = Result.Failed,
                    CodeartifactPackageType = "nuget"
                });
                return new ActionResults().WithSuccess(false);
            }
            shellProvider.UpdateStatus(string.Format("Copied NuGet Source to clipboard: {0}/{1}", domainName, repoName));
            _telemetryLogger.RecordCodeartifactGetRepoUrl(new CodeartifactGetRepoUrl()
            {
                Result = Result.Succeeded,
                CodeartifactPackageType = "nuget"
            });
            return new ActionResults().WithSuccess(true);
        }

        public string GenerateURL(IAmazonCodeArtifact codeArtifactClient, string domainName, string repoName)
        { 
            var request = new GetRepositoryEndpointRequest()
            {
                Domain = domainName,
                Repository = repoName,
            };

            request.Format = PackageFormat.Nuget;
            try
            {
                var endpointUrl = codeArtifactClient.GetRepositoryEndpoint(request).RepositoryEndpoint;
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
