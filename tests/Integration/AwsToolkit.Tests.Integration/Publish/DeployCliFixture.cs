using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Install;
using Amazon.AWSToolkit.Publish.NuGet;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    public class DeployCliFixture
    {
        public static readonly IEnumerable<object[]> SampleInstallationSubfolders = new List<object[]>
        {
            new object[] {""},
            new object[] {"folder with spaces"},
        };

        public InstallOptions InstallOptions { get; private set; }

        public async Task<InstallResult> InstallFromNuGetAsync(string destinationPath, string versionRange)
        {
            InstallOptions = new InstallOptions(destinationPath, versionRange);
            var installer = new DeployCliInstaller(InstallOptions, new NuGetRepository());
            return await installer.InstallAsync(CancellationToken.None);
        }

        public async Task<InstallResult> InstallCurrentVersionFromNuGetAsync(string destinationPath)
        {
            return await InstallFromNuGetAsync(destinationPath, "0.*");
        }

        public Task<InstallResult> InstallOlderVersionFromNuGetAsync(string destinationPath)
        {
            return InstallFromNuGetAsync(destinationPath, "0.10.6");
        }

        /// <summary>
        /// Convenience method used to manually test local builds of the CLI (which may not be published to NuGet)
        /// </summary>
        public void CopyFromLocalPath(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath, true);
            InstallOptions = new InstallOptions(destinationPath, "0.*");
        }

        public bool IsDeployCLIInstalled()
        {
            return File.Exists($@"{InstallOptions.ToolPath}\dotnet-aws.exe");
        }

    }
}
