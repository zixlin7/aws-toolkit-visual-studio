using System;
using System.Linq;

namespace Amazon.AWSToolkit.CommonValidators
{
    public class EcsPlatformVersionValidator
    {
        /// <summary>
        /// Returns empty string if valid, otherwise a validation message 
        /// </summary>
        /// <param name="ecsRepoName">Text to validate</param>
        public static string Validate(string platformVersion)
        {
            if (string.IsNullOrWhiteSpace(platformVersion))
            {
                return "Platform Version cannot be empty";
            }

            if (!string.Equals(platformVersion, "LATEST", StringComparison.OrdinalIgnoreCase))
            {
                var versionParts = platformVersion.Split('.').ToList();
                var isNotNumber = versionParts.Any(data => !int.TryParse(data, out var n));
                if (versionParts.Count != 3 || isNotNumber)
                {
                    return "Platform version must be a specific version like `1.4.0` or `LATEST`";
                }
            }

            return string.Empty;
        }
    }
}
