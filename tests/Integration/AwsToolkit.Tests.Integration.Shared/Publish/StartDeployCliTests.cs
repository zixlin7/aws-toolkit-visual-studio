using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Publish.Services;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.AWSToolkit.Tests.Common.Settings.Publish;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    public class StartDeployCliTests : IAsyncLifetime
    {
        private readonly IPublishSettingsRepository
            _publishSettingsRepository = new InMemoryPublishSettingsRepository();
        private readonly IAWSToolkitShellProvider _toolkitHost = new NoOpToolkitShellProvider();

        private readonly PublishSettings _publishSettings = PublishSettings.CreateDefault();
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly DeployCliFixture _deployCliFixture = new DeployCliFixture();
        private CliServer _cliServer;

        public static readonly IEnumerable<object[]> TestLocationSubfolders = DeployCliFixture.SampleInstallationSubfolders;

        public StartDeployCliTests()
        {
            _publishSettingsRepository.Save(_publishSettings);
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        [Theory]
        [MemberData(nameof(TestLocationSubfolders))]
        public async Task ShouldStartFromLocation(string testLocationSubfolder)
        {
            await _deployCliFixture.InstallCurrentVersionFromNuGetAsync(GetInstallFolder(testLocationSubfolder));
            await CreateCliServer();

            await StartCliServer();
        }

        private async Task CreateCliServer()
        {
            _cliServer = await CliServerFactory.CreateAsync(_deployCliFixture.InstallOptions,
                _publishSettingsRepository, _toolkitHost);
        }

        protected async Task StartCliServer()
        {
            await _cliServer.StartAsync(CancellationToken.None);
        }

        public async Task DisposeAsync()
        {
            _cliServer?.Stop();
            _cliServer?.Dispose();
            TerminateCli();

            // Give the process some time to terminate
            await Task.Delay(2000);

            _testLocation.Dispose();
        }

        /// <summary>
        /// This is a workaround to find and terminate processes started by this test, because
        /// the deploy cli does not provide a way to shut down the server.
        /// </summary>
        private void TerminateCli()
        {
            var processIds = GetTestCliProcessIds();

            Process.GetProcesses()
                .Where(p => processIds.Contains(p.Id))
                .ToList()
                .ForEach(p => p.Kill());
        }

        private IEnumerable<int> GetTestCliProcessIds()
        {
            var processIds = new List<int>();

            // https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-process
            var wmiQueryString = "SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE ExecutablePath IS NOT NULL";
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            using (var results = searcher.Get())
            {
                processIds.AddRange(results.Cast<ManagementObject>()
                    .Where(IsTestDeployCli)
                    .Select(mo => (int) (uint) mo["ProcessId"]));
            }

            return processIds;
        }

        private bool IsTestDeployCli(ManagementObject mo)
        {
            if (mo == null) return false;

            var exePath = (string) mo["ExecutablePath"];

            return !string.IsNullOrWhiteSpace(exePath) &&
                   exePath.Contains(_deployCliFixture.InstallOptions.ToolPath) &&
                   exePath.Contains("dotnet-aws");
        }

        private string GetInstallFolder(string testLocationSubfolder)
        {
            return Path.Combine(_testLocation.TestFolder, testLocationSubfolder);
        }
    }
}
