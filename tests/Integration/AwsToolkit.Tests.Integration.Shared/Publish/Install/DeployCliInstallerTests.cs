using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Install;
using Amazon.AWSToolkit.Tests.Common.IO;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish.Install
{
    public class DeployCliInstallerTests : IDisposable
    {
        public static readonly IEnumerable<object[]> TestLocationSubfolders = DeployCliFixture.SampleInstallationSubfolders;

        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly DeployCliFixture _deployCliFixture = new DeployCliFixture();

        public void Dispose()
        {
            _testLocation.Dispose();
        }

        [Theory]
        [MemberData(nameof(TestLocationSubfolders))]
        public async Task ShouldInstallCli(string testLocationSubfolder)
        {
            // arrange
            var installFolder = GetInstallFolder(testLocationSubfolder);

            // act.
            var result = await _deployCliFixture.InstallFromNuGetAsync(installFolder, "0.*");

            // assert.
            Assert.True(_deployCliFixture.IsDeployCLIInstalled());
            Assert.Equal(InstallResult.Installed, result);
        }

        [Theory]
        [MemberData(nameof(TestLocationSubfolders))]
        public async Task ShouldUpdateExistingCli(string testLocationSubfolder)
        {
            // arrange
            var installFolder = GetInstallFolder(testLocationSubfolder);
            await _deployCliFixture.InstallOlderVersionFromNuGetAsync(installFolder);

            // act.
            var result = await _deployCliFixture.InstallFromNuGetAsync(installFolder, "0.*");

            // assert.
            Assert.True(_deployCliFixture.IsDeployCLIInstalled());
            Assert.Equal(InstallResult.Updated, result);
        }

        [Theory]
        [MemberData(nameof(TestLocationSubfolders))]
        public async Task ShouldSkipUpdatingCli(string testLocationSubfolder)
        {
            // arrange.
            var installFolder = GetInstallFolder(testLocationSubfolder);
            await _deployCliFixture.InstallOlderVersionFromNuGetAsync(installFolder);

            // act.
            var result = await _deployCliFixture.InstallOlderVersionFromNuGetAsync(installFolder);

            // assert.
            Assert.True(_deployCliFixture.IsDeployCLIInstalled());
            Assert.Equal(InstallResult.Skipped, result);
        }

        private string GetInstallFolder(string testLocationSubfolder)
        {
            return Path.Combine(_testLocation.TestFolder, testLocationSubfolder);
        }
    }
}
