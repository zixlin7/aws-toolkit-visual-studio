using System;
using System.Text.RegularExpressions;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    /// <summary>
    /// Utility methods to use with Credentials Profile names
    /// </summary>
    public static class ProfileName
    {
        public const string SsoSessionProfilePrefix = "sso-session";

        /// <summary>
        /// Regex:
        /// - Starts with "sso-session"
        /// - followed by one or more whitespaces
        /// - followed by one or more non-whitespaces, in a group called "name"
        /// </summary>
        private static readonly Regex SsoSessionProfileName = new Regex($"^{SsoSessionProfilePrefix}\\s+(?<name>\\S+)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        /// <summary>
        /// Whether or not the profile name represents an SSO Session (prefixed with "sso-session")
        /// </summary>
        public static bool IsSsoSession(string profileName) => SsoSessionProfileName.IsMatch(profileName);

        /// <summary>
        /// Transforms "sso-session foo" to "foo"
        /// </summary>
        public static string GetSsoSessionFromProfileName(string profileName)
        {
            var match = SsoSessionProfileName.Match(profileName);
            if (!match.Success)
            {
                throw new ArgumentException($"Not a SSO Session based profile name: {profileName}", nameof(profileName));
            }

            return match.Groups["name"].Value;
        }

        /// <summary>
        /// Transforms "foo" to "sso-session foo"
        /// </summary>
        public static string CreateSsoSessionProfileName(string ssoSessionName) =>
            $"{SsoSessionProfilePrefix} {ssoSessionName}";
    }
}
