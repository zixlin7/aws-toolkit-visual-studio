using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Models;

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

        Task<IList<ConfigurationDetail>> GetConfigSettings(string sessionId, CancellationToken cancellationToken);

        Task<ApplyConfigSettingsOutput> ApplyConfigSettings(string sessionId, ConfigurationDetail configurationDetail, CancellationToken cancellationToken);

        Task<ApplyConfigSettingsOutput> ApplyConfigSettings(string sessionId,
            IList<ConfigurationDetail> configurationDetails, CancellationToken cancellationToken);

        Task SetDeploymentTarget(string sessionId, string stackName, string recipeId, bool isRepublish, CancellationToken cancellationToken);

        Task<GetDeploymentDetailsOutput> GetDeploymentDetails(string sessionId, CancellationToken cancellationToken);

        Task<HealthStatusOutput> HealthAsync();
    }

    public class DeployToolController : IDeployToolController
    {
        private readonly IRestAPIClient _client;
        private readonly ConcurrentDictionary<string, RecipeSummary> _recipeSummaries = new ConcurrentDictionary<string, RecipeSummary>();

        public DeployToolController(IRestAPIClient client)
        {
            _client = client;
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

        public async Task<GetDeploymentDetailsOutput> GetDeploymentDetails(string sessionId, CancellationToken cancellationToken)
        {
            return await _client.GetDeploymentDetailsAsync(sessionId, cancellationToken);
        }

        public async Task<IList<ConfigurationDetail>> GetConfigSettings(string sessionId, CancellationToken cancellationToken)
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
                configSettings.AddRange(response.OptionSettings
                    .Select(setting => setting.ToConfigurationDetail())
                );
            }

            return configSettings;
        }

        public async Task<ApplyConfigSettingsOutput> ApplyConfigSettings(string sessionId, ConfigurationDetail configurationDetail, CancellationToken cancellationToken)
        {
           return await ApplyConfigSettings(sessionId, new List<ConfigurationDetail> {configurationDetail},
               cancellationToken);
        }

        public async Task<ApplyConfigSettingsOutput> ApplyConfigSettings(string sessionId, IList<ConfigurationDetail> configurationDetails, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new InvalidSessionIdException($"The Session Id '{sessionId}' is invalid.");
            }
            
            var configSettings = configurationDetails
                .SelectMany(detail => detail.GetSelfAndDescendants())
                .Where(detail => detail.IsLeaf())
                .ToDictionary(x => x.GetLeafId(), x => x.Value.ToString());

            var input = new ApplyConfigSettingsInput {UpdatedSettings = configSettings};

            var response = await _client.ApplyConfigSettingsAsync(sessionId, input, cancellationToken)
                .ConfigureAwait(false);

            return response;
        }

        public async Task SetDeploymentTarget(string sessionId, string stackName, string recipeId, bool isRepublish, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new InvalidSessionIdException($"The Session Id '{sessionId}' is invalid.");

            if (string.IsNullOrWhiteSpace(stackName))
                throw new InvalidParameterException($"The Stack Name '{stackName}' is invalid.");

            if (!isRepublish && string.IsNullOrWhiteSpace(recipeId))
                throw new InvalidParameterException($"The Recipe Id '{recipeId}' is invalid.");

            var setTargetRequest = CreateSetDeploymentTargetInput(stackName, recipeId, isRepublish);

            await _client.SetDeploymentTargetAsync(sessionId, setTargetRequest, cancellationToken).ConfigureAwait(false);
        }

        public Task<HealthStatusOutput> HealthAsync()
        {
            return _client.HealthAsync();
        }

        private SetDeploymentTargetInput CreateSetDeploymentTargetInput(string stackName, string recipeId,
            bool isRepublish)
        {
            if (isRepublish)
            {
                return new SetDeploymentTargetInput { ExistingDeploymentName = stackName };
            }

            return new SetDeploymentTargetInput { NewDeploymentName = stackName, NewDeploymentRecipeId = recipeId };
        }
    }

    public static class DeployToolControllerExtensions
    {
        public static ConfigurationDetail ToConfigurationDetail(this OptionSettingItemSummary optionSettingItem, ConfigurationDetail parentDetail = null)
        {
            var detail = new ConfigurationDetail {
                Id = optionSettingItem.Id,
                Name = optionSettingItem.Name,
                Description = optionSettingItem.Description,
                Type = GetOptionSettingItemType(optionSettingItem.Type),
                TypeHint = optionSettingItem.TypeHint,
                DefaultValue = optionSettingItem.Value,
                // TODO : use category once API provides it. (View is already set to render it)
                Category = string.Empty,
                Advanced = optionSettingItem.Advanced,
                ReadOnly = optionSettingItem.ReadOnly,
                Visible = optionSettingItem.Visible,
                SummaryDisplayable = optionSettingItem.SummaryDisplayable,
                Parent = parentDetail,
            };

            detail.ValueMappings = GetValueMappings(optionSettingItem);
            detail.Value = GetValue(optionSettingItem, detail);

            // Recurse all child data
            if (optionSettingItem.Type.Equals("Object"))
            {
                optionSettingItem.ChildOptionSettings?
                    .Select(child => child.ToConfigurationDetail(detail))
                    .ToList()
                    .ForEach(detail.Children.Add);
            }

            return detail;
        }

        private static object GetValue(OptionSettingItemSummary optionSettingItem, ConfigurationDetail detail)
        {
            if (detail.ValueMappings?.Any() ?? false)
            {
               return Convert.ToString(optionSettingItem.Value, CultureInfo.InvariantCulture);
            }
            return optionSettingItem.Value;
        }

        private static IDictionary<string, string> GetValueMappings(OptionSettingItemSummary optionSettingItem)
        {

            if (optionSettingItem?.ValueMapping?.Any() ?? false)
            {
                return optionSettingItem.ValueMapping;
            }

            if (optionSettingItem?.AllowedValues?.Any() ?? false)
            {
                return optionSettingItem.AllowedValues.ToDictionary(x => x, x => x);
            }

            return new Dictionary<string, string>();
        }

        private static Type GetOptionSettingItemType(string type)
        {
            switch (type)
            {
                case "String":
                    return typeof(string);
                case "Int":
                    return typeof(int);
                case "Double":
                    return typeof(double);
                case "Bool":
                    return typeof(bool);
                case "Object":
                    return typeof(object);
                default:
                    throw new UnsupportedOptionSettingItemTypeException($"The Type '{type}' is not supported.");
            }
        }
    }
}
