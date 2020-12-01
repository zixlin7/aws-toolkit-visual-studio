using System;
using System.Text.RegularExpressions;

namespace Amazon.AWSToolkit.CommonValidators
{
    public class EcsRepoNameValidator
    {
        /// <summary>
        /// Regex explanation:
        /// - starts with a lowercase letter
        /// - followed by 0-255 of the following:
        ///   - lowercase letters
        ///   - numbers
        ///   - hyphens
        ///   - underscores
        ///   - forward slashes
        /// </summary>
        private static readonly Regex
            RepoNameRegex = new Regex(@"^[a-z]([a-z0-9\-_\/]){0,255}$", RegexOptions.Compiled);

        /// <summary>
        /// Returns empty string if valid, otherwise a validation message 
        /// </summary>
        /// <param name="ecsRepoName">Text to validate</param>
        public static string Validate(string ecsRepoName)
        {
            if (string.IsNullOrWhiteSpace(ecsRepoName))
            {
                return "Name cannot be empty";
            }

            if (!RepoNameRegex.IsMatch(ecsRepoName))
            {
                return
                    $"The name must start with a letter and can only contain lowercase letters, numbers, hyphens, underscores, and forward slashes.{Environment.NewLine}Max length: 256";
            }

            return string.Empty;
        }
    }
}