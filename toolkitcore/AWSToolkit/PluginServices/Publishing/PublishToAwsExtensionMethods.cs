using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.PluginServices.Publishing
{
    /// <summary>
    /// General Publish to AWS related extension methods
    /// </summary>
    public static class PublishToAwsExtensionMethods
    {
        /// <summary>
        /// Whether or not this version of Visual Studio supports the Publish to AWS feature.
        /// </summary>
        public static bool SupportsPublishToAwsExperience(this IToolkitHostInfo hostInfo)
        {
            // Publish to AWS is implemented with .NET 4.7.2, which VS 2017 doesn't target.
            // Only activate this functionality for VS 2019 and newer versions.
            if (hostInfo == ToolkitHosts.Vs2017)
            {
                return false;
            }

            return true;
        }
    }
}
