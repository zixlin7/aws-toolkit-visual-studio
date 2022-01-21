using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Publish.Models.Configuration;

namespace Amazon.AWSToolkit.Publish.Models
{
    class SummaryGenerator
    {
        private readonly bool _isRepublish;

        internal SummaryGenerator(bool isRepublish)
        {
            _isRepublish = isRepublish;
        }

        /// <summary>
        /// Generates summary for a list of configuration details filtered by conditions determined by
        /// whether it is for a republish/publish target with appropriate indentation based on node level
        /// </summary>
        /// <param name="details">Collection of details</param>
        /// <param name="nodeLevel">determines level of a config detail</param>
        /// <returns></returns>
        internal string GenerateSummary(IEnumerable<ConfigurationDetail> details, int nodeLevel)
        {
            var fullSummary = new StringBuilder();

            details?.Where(IsConfigDetailDisplayable)
                .Select(detail => GenerateSummary(detail, nodeLevel))
                .Where(summary => !string.IsNullOrWhiteSpace(summary))
                .ToList()
                .ForEach(summary => fullSummary.Append(summary));

            return fullSummary.ToString();
        }

        private bool IsConfigDetailDisplayable(ConfigurationDetail detail)
        {
            return _isRepublish ? detail.SummaryDisplayable : detail.Visible && !detail.Advanced;
        }

        private string GenerateSummary(ConfigurationDetail detail, int nodeLevel)
        {
            return detail.IsLeaf()
                ? GenerateSummaryForLeaf(detail, nodeLevel)
                : GenerateSummaryForParent(detail, nodeLevel);
        }

        private string GenerateSummaryForLeaf(ConfigurationDetail detail, int nodeLevel)
        {
            return $"{CreateIndent(nodeLevel)}{AsSummary(detail)}{Environment.NewLine}";
        }

        private string GenerateSummaryForParent(ConfigurationDetail detail, int nodeLevel)
        {
            var childSummary = GenerateSummary(detail.Children, nodeLevel + 1);
            if (string.IsNullOrWhiteSpace(childSummary))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"{CreateIndent(nodeLevel)}{detail.Name}:");
            sb.Append(childSummary);

            return sb.ToString();
        }

        private string CreateIndent(int level)
        {
            return string.Concat(Enumerable.Repeat("    ", level));
        }

        private  string AsSummary(ConfigurationDetail detail)
        {
            if (detail.Type == DetailType.Boolean && detail.Value.Equals(true))
            {
                return detail.Name;
            }

            return $"{detail.Name}: {detail.Value}";
        }
    }
}
