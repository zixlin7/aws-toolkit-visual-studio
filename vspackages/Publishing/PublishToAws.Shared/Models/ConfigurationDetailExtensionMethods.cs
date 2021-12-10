using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Publish.Models.Configuration;

namespace Amazon.AWSToolkit.Publish.Models
{
    public static class ConfigurationDetailExtensionMethods
    {

        /// <summary>
        /// Retrieves the nodes in a given enumerable and all of their children, recursively.
        /// </summary>
        /// <param name="details">Collection of details to traverse.</param>
        /// <param name="filter">Optional filter to specify which nodes are included. Children are not traversed for excluded nodes.</param>
        public static IEnumerable<ConfigurationDetail> GetDetailAndDescendants(this IEnumerable<ConfigurationDetail> details, Predicate<ConfigurationDetail> filter = null)
        {
            if (details == null)
            {
                return Enumerable.Empty<ConfigurationDetail>();
            }

            return details.SelectMany(detail => detail.GetSelfAndDescendants(filter));
        }

        /// <summary>
        /// Generates summary for a list of configuration details filtered by conditions determined by
        /// whether it is for a republish/publish target with appropriate indentation based on node level
        /// </summary>
        /// <param name="details">Collection of details</param>
        /// <param name="isRepublish">republish or publish target details</param>
        /// <returns></returns>
        public static string GenerateSummary(this IEnumerable<ConfigurationDetail> details,
            bool isRepublish)
        {
            var generator = new SummaryGenerator(isRepublish);
            return generator.GenerateSummary(details, 0);
        }

        public static void ApplyValidationErrors(this ConfigurationDetail detail, ValidationResult validationResult)
        {
            detail.GetSelfAndDescendants()
                .Where(d => validationResult.HasError(d.GetLeafId()))
                .ToList()
                .ForEach(d => d.ValidationMessage = validationResult.GetError(d.GetLeafId()));
        }
    }
}
