using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Utils
{
    public class SolutionStackUtils
    {
        private static readonly Version MinimumNetCoreSolutionStack = new Version("1.2.0");
        private static readonly Version SolutionStackFallbackVersion = new Version("1.0.0");

        /// <summary>
        /// Regex:
        /// version starts with a space, then a 'v', then has three numeric sections in the format "x.y.z"
        /// the version goes into a group named "version"
        /// </summary>
        private static readonly Regex VersionSubstring = new Regex(".* v(?<version>\\d+\\.\\d+\\.\\d+) .*",
            RegexOptions.Compiled, TimeSpan.FromSeconds(3));

        public static bool SolutionStackIsWindows(string solutionStackName)
        {
            return !string.IsNullOrEmpty(solutionStackName) && solutionStackName.Contains(" Windows ");
        }

        public static bool SolutionStackIsLegacy(string solutionStackName)
        {
            return !string.IsNullOrEmpty(solutionStackName) && solutionStackName.Contains("legacy");
        }

        /// <summary>
        /// Traditional asp.net project
        /// </summary>
        public static bool SolutionStackSupportsDotNetFramework(string solutionStackName)
        {
            return SolutionStackIsWindows(solutionStackName);
        }

        public static bool SolutionStackSupportsDotNetCore(string solutionStackName)
        {
            if (SolutionStackIsWindows(solutionStackName))
            {
                var version = ParseVersionFromSolutionStack(solutionStackName, SolutionStackFallbackVersion);
                return MinimumNetCoreSolutionStack.CompareTo(version) <= 0;
            }

            return SolutionStackIsDotNetCore(solutionStackName);
        }

        /// <example>
        /// 64bit Windows Server Core 2019 v2.5.6 running IIS 10.0 -> Version 2.5.6
        /// </example>
        public static Version ParseVersionFromSolutionStack(string solutionStackName, Version defaultVersion)
        {
            if (string.IsNullOrEmpty(solutionStackName))
            {
                return defaultVersion;
            }

            var tokens = solutionStackName.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            var parsedVersion = tokens
                .Where(token => token.StartsWith("v") && token.Contains("."))
                .Select(token =>
                {
                    if (Version.TryParse(token.Substring(1), out var version))
                    {
                        return version;
                    }

                    return defaultVersion;
                }).FirstOrDefault();

            return parsedVersion ?? defaultVersion;
        }

        // TODO : Confirm actual Platform Name closer to release
        private static readonly string[] DotNetCorePlatformNames = new string[]
        {
            "DotNetCore",
            ".NET Core"
        };

        private static bool SolutionStackIsDotNetCore(string solutionStackName)
        {
            if (string.IsNullOrEmpty(solutionStackName))
            {
                return false;
            }

            return DotNetCorePlatformNames.Any(name =>
                solutionStackName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1);
        }

        /// <summary>
        /// Tries to parse a Version from the Beanstalk stack names
        /// </summary>
        /// <example>
        /// 64bit Windows Server Core 2019 v2.5.6 running IIS 10.0 -> (2.5.6)
        /// </example>
        public static bool TryGetVersion(string solutionStackName, out Version version)
        {
            version = null;

            var regexMatch = VersionSubstring.Match(solutionStackName);

            if (!regexMatch.Success)
            {
                return false;
            }

            var versionStr = regexMatch.Groups["version"].Value;
            return Version.TryParse(versionStr, out version);
        }
    }
}
