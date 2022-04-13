using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.Publish.Install
{
    public class InstallOptionsFactory
    {
       private const string _versionRange = "0.38.3";

        public static InstallOptions Create(IToolkitHostInfo toolkitHostInfo)
        {
            return new InstallOptions(GetInstallPath(toolkitHostInfo), _versionRange); 
        }

        private static string GetInstallPath(IToolkitHostInfo toolkitHostInfo)
        {
            return ToolkitAppDataPath.Join($@"aws.deploy.cli\{toolkitHostInfo.Version}");
        }
    }
}
