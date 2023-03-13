using Amazon.AWSToolkit.Publish.Util;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.Publish.Install
{
    public class InstallOptionsFactory
    {
        private const string VersionRange = "1.11.6";

        public static InstallOptions Create(IToolkitHostInfo toolkitHostInfo)
        {
            return new InstallOptions(GetInstallPath(toolkitHostInfo), VersionRange); 
        }

        private static string GetInstallPath(IToolkitHostInfo toolkitHostInfo)
        {
            return ToolkitAppDataPath.Join($@"{PublishToAwsConstants.DeployToolPackageName}\{toolkitHostInfo.Version}");
        }
    }
}
