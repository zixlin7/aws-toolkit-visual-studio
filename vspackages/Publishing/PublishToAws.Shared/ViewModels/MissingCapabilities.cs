using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Publish.Models;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    public class MissingCapabilities
    {
        public readonly HashSet<string> Missing = new HashSet<string>();
        public readonly HashSet<string> Resolved = new HashSet<string>();

        private readonly Dictionary<string, HashSet<string>> _recipeToMissingCapabilities = new Dictionary<string, HashSet<string>>();

        public void Update(string recipeId, IList<TargetSystemCapability> systemCapabilities)
        {
            var missingCapabilities = systemCapabilities.Select(c => c.Name).ToList();
            UpdateMissing(missingCapabilities);
            UpdateResolved(recipeId, missingCapabilities);
        }

        private void UpdateMissing(IList<string> missingCapabilities) => Missing.UnionWith(missingCapabilities);

        private void UpdateResolved(string recipeId, IList<string> missingCapabilities)
        {
            var missingCapabilitiesForRecipe = GetMissingCapabilitiesFor(recipeId);

            missingCapabilitiesForRecipe.UnionWith(missingCapabilities);
            _recipeToMissingCapabilities[recipeId] = missingCapabilitiesForRecipe;

            missingCapabilitiesForRecipe
                .Except(missingCapabilities)
                .ToList()
                .ForEach(c => Resolved.Add(c));
        }

        private HashSet<string> GetMissingCapabilitiesFor(string publishRecipeId)
        {
            if (string.IsNullOrEmpty(publishRecipeId) || !_recipeToMissingCapabilities.ContainsKey(publishRecipeId))
            {
                return new HashSet<string>();
            }

            return _recipeToMissingCapabilities[publishRecipeId];
        }
    }
}
