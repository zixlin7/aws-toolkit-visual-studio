using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Install;
using Amazon.AWSToolkit.Publish.NuGet;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.AWSToolkit.Tests.Common.TestExtensions;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish.Install
{
    public class DeployCliInstallerTests : IDisposable
    {

        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();

        public void Dispose()
        {
            _testLocation.Dispose();
        }

        [Vs2019OrLaterFact]
        public async Task ShouldInstallCli()
        {
            // act.
            var result = await InstallWithVersionRangeAsync("0.*");

            // assert.
            Assert.True(IsDeployCLIInstalled());
            Assert.Equal(InstallResult.Installed, result);
        }

        private Task<InstallResult> InstallWithVersionRangeAsync(string versionRange)
        {
            var options = new InstallOptions(_testLocation.TestFolder, versionRange);
            var installer = CreateInstaller(options);
            return installer.InstallAsync(CancellationToken.None);
        }

        private DeployCliInstaller CreateInstaller(InstallOptions options)
        {
            return new DeployCliInstaller(options, new NuGetRepository());
        }

        private bool IsDeployCLIInstalled()
        {
            return File.Exists($@"{_testLocation.TestFolder}\dotnet-aws.exe");
        }

        [Vs2019OrLaterFact]
        public async Task ShouldUpdateExistingCli()
        {
            // arrange
            await InstallOlderVersionOfCliAsync();

            // act.
            var result = await InstallWithVersionRangeAsync("0.*");

            // assert.
            Assert.True(IsDeployCLIInstalled());
            Assert.Equal(InstallResult.Updated, result);
        }

        private Task<InstallResult> InstallOlderVersionOfCliAsync()
        {
            return InstallWithVersionRangeAsync("0.10.6");
        }

        [Vs2019OrLaterFact]
        public async Task ShouldSkipUpdatingCli()
        {
            // arrange.
            await InstallOlderVersionOfCliAsync();

            // act.
            var result = await InstallOlderVersionOfCliAsync();

            // assert.
            Assert.True(IsDeployCLIInstalled());
            Assert.Equal(InstallResult.Skipped, result);
        }
    }
}
