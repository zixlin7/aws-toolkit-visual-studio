using System;
using System.Linq;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Utils
{
    public class SolutionStackUtils
    {
        private static readonly Version MinimumNetCoreSolutionStack = new Version("1.2.0");
        private static readonly Version SolutionStackFallbackVersion = new Version("1.0.0");

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
    }
}