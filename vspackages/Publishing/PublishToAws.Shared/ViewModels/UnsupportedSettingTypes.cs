using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    /// <summary>
    /// Represents setting types that are not supported by the Publish to AWS experience
    /// for eg. KeyValue
    /// </summary>
    public class UnsupportedSettingTypes
    {
        public  Dictionary<string, HashSet<string>> RecipeToUnsupportedSetting =
            new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Updates the mapping of recipe to set of unsupported setting types
        /// </summary>
        /// <param name="recipeId"></param>
        /// <param name="configDetails"></param>
        public void Update(string recipeId, IList<ConfigurationDetail> configDetails)
        {
            var unsupportedSettingTypes = configDetails?.Where(x => x.Type == typeof(UnsupportedType))
                .Select(x => x.OriginalType).ToHashSet();
            Update(recipeId, unsupportedSettingTypes);
        }

        /// <summary>
        /// Records the `publish_unsupportedSetting` metric for each pair of recipeId and settingType
        /// stored in the <see cref="RecipeToUnsupportedSetting"/> dictionary
        /// </summary>
        /// <param name="publishContext"></param>
        public void RecordMetric(PublishApplicationContext publishContext)
        {
            RecipeToUnsupportedSetting.ToList().ForEach(settingEntry =>
                RecordMetricForRecipe(publishContext, settingEntry.Key, settingEntry.Value));
        }

        private void RecordMetricForRecipe(PublishApplicationContext publishContext, string recipeId,
            HashSet<string> settingTypes)
        {
            settingTypes.ToList().ForEach(settingType =>
            {
                publishContext.TelemetryLogger.RecordPublishUnsupportedSetting(new PublishUnsupportedSetting()
                {
                    AwsAccount = publishContext.ConnectionManager.ActiveAccountId,
                    AwsRegion = publishContext.ConnectionManager.ActiveRegion?.Id,
                    RecipeId = recipeId,
                    PublishSettingType = settingType
                });
            });
        }

        private void Update(string recipeId, HashSet<string> settingTypes)
        {
            if (string.IsNullOrWhiteSpace(recipeId) || HasNoSettingTypes(settingTypes))
            {
                return;
            }
            var unsupportedSettingsForRecipe = GetUnsupportedSettingTypes(recipeId);

            unsupportedSettingsForRecipe.UnionWith(settingTypes);
            RecipeToUnsupportedSetting[recipeId] = unsupportedSettingsForRecipe;
        }

        private bool HasNoSettingTypes(HashSet<string> settingTypes)
        {
            return !settingTypes?.Any() ?? true;
        }

        private HashSet<string> GetUnsupportedSettingTypes(string recipeId)
        {
            if (!RecipeToUnsupportedSetting.ContainsKey(recipeId))
            {
                return new HashSet<string>();
            }

            return RecipeToUnsupportedSetting[recipeId];
        }
    }
}
