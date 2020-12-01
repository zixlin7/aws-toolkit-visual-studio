using System;
using System.Text.RegularExpressions;

namespace Amazon.AWSToolkit.CommonValidators
{
    public class DockerTagValidator
    {
        /// <summary>
        /// Regex explanation:
        /// - starts with a lowercase or uppercase letter or numbers or underscores
        /// - followed by 0-127 of the following:
        ///   - lowercase or uppercase letters
        ///   - numbers
        ///   - underscores
        ///   - periods
        ///   - hyphens
        /// </summary>
        private static readonly Regex
            TagRegex = new Regex(@"^[a-zA-Z0-9_]([a-zA-Z0-9_\.\-]){0,127}$", RegexOptions.Compiled);

        /// <summary>
        /// Returns empty string if valid, otherwise a validation message 
        /// </summary>
        /// <param name="tag">Text to validate</param>
        public static string Validate(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return "Tag cannot be empty";
            }

            if (!TagRegex.IsMatch(tag))
            {
                return
                    $"The tag can only contain lowercase and uppercase letters, numbers, underscores, periods, and hyphens.{Environment.NewLine}The tag cannot start with a period or hyphen.{Environment.NewLine}Max length: 128";
            }

            return string.Empty;
        }
    }
}