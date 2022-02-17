using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Install;
using Amazon.AWSToolkit.Tests.Common.IO;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish.Install
{
    public class VerifyDeployCliTests : IDisposable
    {
        public static readonly IEnumerable<object[]> TestLocationSubfolders = DeployCliFixture.SampleInstallationSubfolders;

        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly DeployCliFixture _deployCliFixture = new DeployCliFixture();

        [Theory]
        [MemberData(nameof(TestLocationSubfolders))]
        public async Task ShouldVerifyCli_Succeeds(string testLocationSubfolder)
        {
            await _deployCliFixture.InstallCurrentVersionFromNuGetAsync(GetInstallFolder(testLocationSubfolder));

            var exception = await Record.ExceptionAsync(async () =>
            {
                await new VerifyDeployCli(_deployCliFixture.InstallOptions).ExecuteAsync(CancellationToken.None);
            });

            Assert.Null(exception);
        }

        [Theory]
        [MemberData(nameof(TestLocationSubfolders))]
        public async Task ShouldVerifyCli_Fails(string testLocationSubfolder)
        {
            var installOptions = new InstallOptions(GetInstallFolder(testLocationSubfolder), "0.*");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new VerifyDeployCli(installOptions).ExecuteAsync(CancellationToken.None);
            });
        }

        public void Dispose()
        {
            _testLocation.Dispose();
        }

        private string GetInstallFolder(string testLocationSubfolder)
        {
            return Path.Combine(_testLocation.TestFolder, testLocationSubfolder);
        }
    }
}
