using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Install;
using Amazon.AWSToolkit.Util;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    /// <summary>
    /// Installs the deploy CLI to the Toolkit-used location
    /// </summary>
    public class DeployCliInstallationFixture : IAsyncLifetime
    {
        public InstallOptions InstallOptions;
        public InstallDeployCli InstallDeployCli;

        public async Task InitializeAsync()
        {
            InstallOptions = InstallOptionsFactory.Create(new ToolkitHostInfo("defaultName", "2022"));
            InstallDeployCli = new InstallDeployCli(InstallOptions);
            await InstallDeployCli.ExecuteAsync(CancellationToken.None);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
