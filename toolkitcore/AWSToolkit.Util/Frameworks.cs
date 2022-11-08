using System;

namespace Amazon.AWSToolkit
{
    public static class Frameworks
    {
        public const string NetFramework47 = "netframework4.7";

        public const string NetCoreApp10 = "netcoreapp1.0";
        public const string NetCoreApp21 = "netcoreapp2.1";
        public const string NetCoreApp31 = "netcoreapp3.1";
        public const string Net50 = "net5.0";
        public const string Net60 = "net6.0";
        public const string Net70 = "net7.0";

        /// <summary>
        /// Utility method to compare against expected framework strings
        /// </summary>
        public static bool MatchesFramework(this string text, string targetFramework)
        {
            return string.Equals(text, targetFramework, StringComparison.OrdinalIgnoreCase);
        }
    }
}
