using System;
using System.IO;

namespace Amazon.AWSToolkit.CodeArtifact.Utils
{

    public class Utilities
    {
        public const string NETCORE_PLUGIN_TYPE = "netcore";
        public const string NETFX_PLUGIN_TYPE = "netfx";

        public static string DetermineInstallPath(string pluginType)
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var filePath = Path.Combine(homeDir, ".nuget", "plugins", pluginType, "AWS.CodeArtifact.NuGetCredentialProvider");

            return filePath;
        }
    }
}