using System;
using System.Text.RegularExpressions;

using Amazon.AWSToolkit.Shell;

using log4net;

namespace Amazon.AWSToolkit.VersionInfo
{
    public static class DotNetVersion
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(DotNetVersion));
        private const int ProcessTimeoutMs = 10000;

        /// <summary>
        /// Assumption: output is in a form like "7.0.100"
        /// Unknown: If separators could be different in other locales (like commas)
        /// Unknown: If 'dotnet --version' will ever add decorative text around the version
        /// 
        /// Behavior: after any amount of whitespace, pull the first number sequence from the text
        /// </summary>
        private static readonly Regex VersionRegex = new Regex("\\s*(?<majorVersion>\\d+).*");

        /// <summary>
        /// Returns the major version number of .NET (Core/5+), or null the version cannot be determined.
        /// Example "7.0.100" -> 7 (.NET 7)
        /// </summary>
        public static int? GetMajorVersion()
        {
            try
            {
                var versionOutput = CallDotNetVersion();
                var match = VersionRegex.Match(versionOutput);
                if (!match.Success)
                {
                    _logger.Error($"Unable to parse version from 'dotnet --version' call. Output: {versionOutput}");
                    return null;
                }

                var majorVersion = match.Groups["majorVersion"].Value;
                return int.Parse(majorVersion);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error calling 'dotnet --version'", ex);
                return null;
            }
        }

        private static string CallDotNetVersion()
        {
            using (var process = DotnetProcess.CreateHeadless("--version"))
            {
                process.Start();

                process.WaitForExit(ProcessTimeoutMs);

                return process.StandardOutput.ReadToEnd().Trim();
            }
        }
    }
}
