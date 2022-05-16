using System.Text.RegularExpressions;

namespace Amazon.AWSToolkit.SNS
{
    public static class SnsValidation
    {
        public const int MaxTopicNameLength = 256;

        /// <summary>
        /// Regex: "Only alphanumeric characters, hyphens, and underscores"
        /// </summary>
        public static readonly Regex InvalidTopicNameRegex = new Regex("[^a-zA-Z0-9\\-_]+", RegexOptions.Compiled);
    }
}
