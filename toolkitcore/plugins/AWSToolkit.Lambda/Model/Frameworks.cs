using System;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public static class Frameworks
    {
        public const string NetCoreApp10 = "netcoreapp1.0";
        public const string NetCoreApp21 = "netcoreapp2.1";
        public const string NetCoreApp31 = "netcoreapp3.1";

        /// <summary>
        /// Utility method to compare against expected framework strings
        /// </summary>
        public static bool MatchesFramework(this string text, string targetFramework)
        {
            return string.Equals(text, targetFramework, StringComparison.OrdinalIgnoreCase);
        }
    }
}