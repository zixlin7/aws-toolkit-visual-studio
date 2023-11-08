using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Settings;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Install
{
    public class CodeWhispererInstallerTests : IDisposable
    {
        private readonly FakeCodeWhispererSettingsRepository _settingsRepository =
            new FakeCodeWhispererSettingsRepository();

        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<ITaskStatusNotifier> _taskNotifier = new Mock<ITaskStatusNotifier>();
        private readonly CodeWhispererInstaller _sut;
        private MetricDatum _metric;

        public CodeWhispererInstallerTests()
        {
            _contextFixture.SetupTelemetryCallback(metrics =>
            {
                var datum = metrics.Data.FirstOrDefault(x => string.Equals(x.MetricName, "languageServer_setup"));
                if (datum != null)
                {
                    _metric = datum;
                }
            });
            _sut = new CodeWhispererInstaller(null, _settingsRepository, _contextFixture.ToolkitContext);
        }

        [Fact]
        public async Task DownloadAsync_WhenCancelled()
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _sut.ExecuteAsync(_taskNotifier.Object, tokenSource.Token));
        }

        [Fact]
        public async Task DownloadAsync_WhenLocalOverride()
        {
            _settingsRepository.Settings.LspSettings.LanguageServerPath = "test-local-path/abc.exe";
            var result = await _sut.ExecuteAsync(_taskNotifier.Object);

            Assert.Equal(_settingsRepository.Settings.LspSettings.LanguageServerPath, result.Path);
            Assert.Equal(LanguageServerLocation.Override, result.Location);

            Assert.Equal(Result.Succeeded.ToString(), _metric.Metadata["result"]);
            Assert.Equal(LanguageServerSetupStage.GetServer.ToString(), _metric.Metadata["languageServerSetupStage"]);
            Assert.Equal(LanguageServerLocation.Override.ToString(), _metric.Metadata["languageServerLocation"]);
        }

        public void Dispose()
        {
            _settingsRepository?.Dispose();
        }
    }
}
