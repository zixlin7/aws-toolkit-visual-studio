using Amazon.AWSToolkit.Publish.NuGet;

namespace Amazon.AWSToolkit.Publish.Install
{
    public class DeployCliInstallerFactory
    {
        public static DeployCliInstaller Create(InstallOptions installOptions)
        {
            return new DeployCliInstaller(installOptions, new NuGetRepository());
        }
    }
}
