using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tasks;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    /// <summary>
    /// A toolkit encapsulation to interact with the aws deploy cli using <see cref="IRestAPIClient"/> 
    /// </summary>
    public interface IDeployToolController
    {
        Task<ICollection<TargetSystemCapability>> GetCompatibilityAsync(string sessionId, CancellationToken cancellationToken);

        Task<SessionDetails> StartSessionAsync(string region, string projectPath, CancellationToken cancellationToken);

        Task StopSessionAsync(string sessionId, CancellationToken cancellationToken);

        Task<ICollection<PublishRecommendation>> GetRecommendationsAsync(string sessionId, string projectPath, CancellationToken cancellationToken);

        Task<ICollection<RepublishTarget>> GetRepublishTargetsAsync(string sessionId, string projectPath, CancellationToken cancellationToken);

        Task StartDeploymentAsync(string sessionId);

        Task<GetDeploymentStatusOutput> GetDeploymentStatusAsync(string sessionId);

        Task<IList<ConfigurationDetail>> GetConfigSettingsAsync(string sessionId, CancellationToken cancellationToken);


        Task<IList<ConfigurationDetail>> UpdateConfigSettingValuesAsync(string sessionId, IEnumerable<ConfigurationDetail> configDetails,
            CancellationToken token);

        Task<Dictionary<string, string>> GetConfigSettingValuesAsync(string sessionId, string configId,
            CancellationToken cancellationToken);

        Task<ValidationResult> ApplyConfigSettingsAsync(string sessionId, ConfigurationDetail configurationDetail, CancellationToken cancellationToken);

        Task<ValidationResult> ApplyConfigSettingsAsync(string sessionId,
            IList<ConfigurationDetail> configurationDetails, CancellationToken cancellationToken);

        /// <summary>
        /// Overload: New deployment
        /// </summary>
        Task SetDeploymentTargetAsync(string sessionId, PublishRecommendation newDeploymentTarget, string stackName, CancellationToken cancellationToken);

        /// <summary>
        /// Overload: Redeployment
        /// </summary>
        Task SetDeploymentTargetAsync(string sessionId, RepublishTarget existingDeploymentTarget, CancellationToken cancellationToken);

        Task<GetDeploymentDetailsOutput> GetDeploymentDetailsAsync(string sessionId, CancellationToken cancellationToken);

        Task<HealthStatusOutput> HealthAsync();
    }

    public class DeployToolController : IDeployToolController
    {
        /// <summary>
        /// Skip loading Value Mappings for these type hints.
        /// This improves the overall performance of configuration detail loading, by
        /// avoiding server requests, which may perform AWS account lookups or other computations.
        /// </summary>
        public static readonly string[] TypeHintLoadingExclusions = new string[]
        {
            // Type Hints with low relevance for Value Mappings
            ConfigurationDetail.TypeHints.DockerBuildArgs,
            ConfigurationDetail.TypeHints.DockerExecutionDirectory,
            ConfigurationDetail.TypeHints.DotnetPublishAdditionalBuildArguments,

            // Type Hints with Custom selection (and loading) UIs in the Toolkit
            ConfigurationDetail.TypeHints.EcrRepository,
            ConfigurationDetail.TypeHints.ExistingIamRole,
            ConfigurationDetail.TypeHints.InstanceType,
        };

        private readonly IAWSToolkitShellProvider _toolkitShell;
        private readonly IRestAPIClient _client;
        private readonly ConfigurationDetailFactory _configurationDetailFactory;

        public DeployToolController(IRestAPIClient client, ConfigurationDetailFactory configurationDetailFactory,
            IAWSToolkitShellProvider toolkitShell)
        {
            _toolkitShell = toolkitShell;
            _client = client;
            _configurationDetailFactory = configurationDetailFactory;
        }

        public async Task<SessionDetails> StartSessionAsync(string region, string projectPath, CancellationToken cancellationToken)
        {
            var request = new StartDeploymentSessionInput { AwsRegion = region, ProjectPath = projectPath };
            var response = await _client.StartDeploymentSessionAsync(request, cancellationToken);

            if (response?.SessionId == null)
            {
                throw new DeployToolException("Deploy Tool did not return a session id");
            }

            return new SessionDetails
            {
                SessionId = response.SessionId,
                DefaultApplicationName = response.DefaultDeploymentName ?? throw new DeployToolException("Deploy Tool did not return a default application name"),
            };
        }

        public async Task StopSessionAsync(string sessionId, CancellationToken token)
        {
            await _client.CloseDeploymentSessionAsync(sessionId, token);
        }

        public async Task<ICollection<PublishRecommendation>> GetRecommendationsAsync(string sessionId, string projectPath, CancellationToken cancellationToken)
        {
            var response = await _client.GetRecommendationsAsync(sessionId, cancellationToken);

            if (response?.Recommendations?.Any() != true)
            {
                return new List<PublishRecommendation>();
            }

            var recommendationSummaries = response.Recommendations;

            var first = recommendationSummaries.First();

            return recommendationSummaries.Select(x => new PublishRecommendation(x)
            {
                IsRecommended = x == first,
            }).ToList();
        }

        public async Task<ICollection<RepublishTarget>> GetRepublishTargetsAsync(string sessionId, string projectPath, CancellationToken cancellationToken)
        {
            var response = await _client.GetExistingDeploymentsAsync(sessionId, cancellationToken);
            if (response?.ExistingDeployments?.Any() != true)
            {
                return new List<RepublishTarget>();
            }

            var existingDeployments = response.ExistingDeployments;

            existingDeployments = existingDeployments
                    .OrderByDescending(x => x.UpdatedByCurrentUser)
                    .ThenByDescending(x => x.LastUpdatedTime)
                    .ToList();

            var first = existingDeployments.First();

            return existingDeployments.Select(x => new RepublishTarget(x)
            {
                IsRecommended = x == first,
                DeployedToRecently = x.UpdatedByCurrentUser
            }).ToList();
        }

        public async Task<ICollection<TargetSystemCapability>> GetCompatibilityAsync(string sessionId, CancellationToken cancellationToken)
        {
            var response = await _client.GetCompatibilityAsync(sessionId, cancellationToken);
            if (response?.Capabilities?.Any() != true)
            {
                return new List<TargetSystemCapability>();
            }

            return response.Capabilities.Select(x => new TargetSystemCapability(x)).ToList();
        }

        public async Task StartDeploymentAsync(string sessionId)
        {
            await _client.StartDeploymentAsync(sessionId);
        }

        public async Task<GetDeploymentStatusOutput> GetDeploymentStatusAsync(string sessionId)
        {
           return await _client.GetDeploymentStatusAsync(sessionId);
        }

        public async Task<GetDeploymentDetailsOutput> GetDeploymentDetailsAsync(string sessionId, CancellationToken cancellationToken)
        {
            return await _client.GetDeploymentDetailsAsync(sessionId, cancellationToken);
        }

        public async Task<IList<ConfigurationDetail>> GetConfigSettingsAsync(string sessionId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new InvalidSessionIdException($"The Session Id '{sessionId}' is invalid.");
            }

            var configSettings = new List<ConfigurationDetail>();

            var response = await _client.GetConfigSettingsAsync(sessionId, cancellationToken)
                    .ConfigureAwait(false);

            if (response?.OptionSettings?.Any() ?? false)
            {
                configSettings.AddRange(response.OptionSettings.Select(_configurationDetailFactory.CreateFrom));
            }

            return configSettings;
        }

        public async Task<IList<ConfigurationDetail>> UpdateConfigSettingValuesAsync(string sessionId,
            IEnumerable<ConfigurationDetail> configDetails,
            CancellationToken token)
        {
            var configurationDetails = configDetails.ToList();

            var detailsToUpdate = configurationDetails
                .SelectMany(d => d.GetSelfAndDescendants())
                .Where(d => d.IsLeaf())
                .Where(d => d.Visible)
                .Where(d => IsTypeHintSupported(d) && !d.HasValueMappings())
                .ToList();

            await Task.WhenAll(detailsToUpdate.Select(async detail =>
                {
                    await GetValueMappingsAsync(detail, sessionId, token);
                }))
                .ConfigureAwait(false);

            return configurationDetails;
        }

        private async Task GetValueMappingsAsync(ConfigurationDetail detail, string sessionId, CancellationToken token)
        {
            try
            {
                var updatedValueMappings = await GetConfigSettingValuesAsync(
                    sessionId, detail.GetLeafId(), token).ConfigureAwait(false);

                token.ThrowIfCancellationRequested();
                detail.ValueMappings = updatedValueMappings;
            }
            catch (ApiException e)
            {
                var innerMessage = e.GetExceptionInnerMessage();
                if (innerMessage.Contains("No such host is known"))
                {
                    innerMessage = $"Service may not be available in this region. {innerMessage}";
                }

                _toolkitShell.OutputToHostConsoleAsync(
                    $"Unable to fetch values for {detail.FullDisplayName}: {innerMessage}",
                    true).LogExceptionAndForget();
            }

        }

        private readonly IEnumerable<DetailType> _supportedTypeHints = new HashSet<DetailType>() { DetailType.List, DetailType.String };

        private bool IsTypeHintSupported(ConfigurationDetail detail)
        {
            if (TypeHintLoadingExclusions.Contains(detail.TypeHint))
            {
                return false;
            }

            return _supportedTypeHints.Contains(detail.Type) && !string.IsNullOrEmpty(detail.TypeHint);
        }

        public async Task<Dictionary<string, string>> GetConfigSettingValuesAsync(string sessionId, string configId,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _client
                    .GetConfigSettingResourcesAsync(sessionId, configId, cancellationToken)
                    .ConfigureAwait(false);
                return response.Resources == null
                    ? new Dictionary<string, string>()
                    : response.Resources.ToList().ToDictionary(resource => resource.SystemName,
                        resource => resource.DisplayName);
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode != 404)
                {
                    throw;
                }
            }

            return new Dictionary<string, string>();
        }

        public async Task<ValidationResult> ApplyConfigSettingsAsync(string sessionId, ConfigurationDetail configurationDetail, CancellationToken cancellationToken)
        {
            return await ApplyConfigSettingsAsync(sessionId, new List<ConfigurationDetail> { configurationDetail }, cancellationToken);
        }

        public async Task<ValidationResult> ApplyConfigSettingsAsync(string sessionId, IList<ConfigurationDetail> configurationDetails, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new InvalidSessionIdException($"The Session Id '{sessionId}' is invalid.");
            }
            
            var configSettings = configurationDetails
                .SelectMany(detail => detail.GetSelfAndDescendants())
                .Where(detail => detail.IsLeaf())
                .Where(detail => !detail.ReadOnly)
                .ToDictionary(x => x.GetLeafId(), x => x.Value.ToString());

            var input = new ApplyConfigSettingsInput {UpdatedSettings = configSettings};

            var response = await _client.ApplyConfigSettingsAsync(sessionId, input, cancellationToken)
                .ConfigureAwait(false);

            var validation = new ValidationResult();

            if (response.FailedConfigUpdates != null)
            {
                foreach (var detailIdToError in response.FailedConfigUpdates)
                {
                    validation.AddError(detailIdToError.Key, detailIdToError.Value);
                }
            }

            return validation;
        }

        /// <summary>
        /// New deployment
        /// </summary>
        public async Task SetDeploymentTargetAsync(string sessionId, PublishRecommendation newDeploymentTarget, string stackName, CancellationToken cancellationToken)
        {
            var recipeId = newDeploymentTarget?.RecipeId;

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new InvalidSessionIdException($"Invalid Session Id: '{sessionId}'");
            }

            if (string.IsNullOrWhiteSpace(stackName))
            {
                throw new InvalidParameterException($"Invalid stack name: '{stackName}'");
            }

            if (string.IsNullOrWhiteSpace(recipeId))
            {
                throw new InvalidParameterException($"Invalid recipe Id: '{recipeId}'");
            }

            var setTargetRequest = new SetDeploymentTargetInput { NewDeploymentName = stackName, NewDeploymentRecipeId = recipeId };

            await InvokeSetDeploymentTargetAsync(sessionId, setTargetRequest, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Redeployment
        /// </summary>
        public async Task SetDeploymentTargetAsync(string sessionId, RepublishTarget existingDeploymentTarget, CancellationToken cancellationToken)
        {
            var existingDeploymentId = existingDeploymentTarget?.ExistingDeploymentId;

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new InvalidSessionIdException($"Invalid Session Id: '{sessionId}'");
            }

            if (string.IsNullOrWhiteSpace(existingDeploymentId))
            {
                throw new InvalidParameterException($"Invalid existing deployment Id: '{existingDeploymentId}'");
            }

            var setTargetRequest = new SetDeploymentTargetInput { ExistingDeploymentId = existingDeploymentId };

            await InvokeSetDeploymentTargetAsync(sessionId, setTargetRequest, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Calls SetDeploymentTargetAsync and wraps exception that are understood by the Toolkit
        /// </summary>
        private async Task InvokeSetDeploymentTargetAsync(string sessionId, SetDeploymentTargetInput setTargetRequest,
            CancellationToken cancellationToken)
        {
            try
            {
                await _client.SetDeploymentTargetAsync(sessionId, setTargetRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (ApiException e)
            {
                if (InvalidApplicationNameException.TryCreate(e as ApiException<ProblemDetails>,
                        out InvalidApplicationNameException invalidApplicationNameException))
                {
                    throw invalidApplicationNameException;
                }

                if (e.StatusCode == 500)
                {
                    throw new DeployToolException("Deploy Tool failed due to internal error.", e);
                }

                throw;
            }
        }

        public Task<HealthStatusOutput> HealthAsync()
        {
            return _client.HealthAsync();
        }
    }
}
