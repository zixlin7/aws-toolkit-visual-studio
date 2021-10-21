using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Install;
using Amazon.AWSToolkit.Publish.NuGet;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.AWSToolkit.Tests.Common.TestExtensions;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish.Install
{
    public class VerifyDeployCliTests : IDisposable
    {
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly InstallOptions _options;

        public VerifyDeployCliTests()
        {
            _options = new InstallOptions(_testLocation.TestFolder, "0.*");
        }

        [Vs2019OrLaterFact]
        public async Task ShouldVerifyCli_Succeeds()
        {
            await InstallCliAsync();

            var exception = await Record.ExceptionAsync(async () =>
            {
                await new VerifyDeployCli(_options).ExecuteAsync(CancellationToken.None);
            });

            Assert.Null(exception);
        }

        [Vs2019OrLaterFact]
        public async Task ShouldVerifyCli_Fails()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new VerifyDeployCli(_options).ExecuteAsync(CancellationToken.None);
            });
        }

        public void Dispose()
        {
            _testLocation.Dispose();
        }

        private Task<InstallResult> InstallCliAsync()
        {
            var installer = new DeployCliInstaller(_options, new NuGetRepository());
            return installer.InstallAsync(CancellationToken.None);
        }
    }
}
