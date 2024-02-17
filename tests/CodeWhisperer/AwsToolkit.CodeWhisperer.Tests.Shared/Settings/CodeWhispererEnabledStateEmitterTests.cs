using System;
using System.Linq;

using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.CodeWhisperer.Tests.TestUtilities;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Microsoft.VisualStudio.Sdk.TestFramework;

using Microsoft.VisualStudio.Shell;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Settings
{
    [Collection(VsMockCollection.CollectionName)]
    public class CodeWhispererEnabledStateEmitterTests : IDisposable
    {
        private const string _codeWhispererEnabledMetricName = "codewhisperer_enabled";
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly FakeCodeWhispererSettingsRepository _settingsRepository = new FakeCodeWhispererSettingsRepository();
        private readonly CodeWhispererEnabledStateEmitter _sut;

        public CodeWhispererEnabledStateEmitterTests(GlobalServiceProvider serviceProvider)
        {
            serviceProvider.Reset();

            var taskFactoryProvider = new ToolkitJoinableTaskFactoryProvider(ThreadHelper.JoinableTaskContext);
            _sut = new CodeWhispererEnabledStateEmitter(_settingsRepository, _toolkitContextFixture.ToolkitContextProvider, taskFactoryProvider);

            _settingsRepository.Settings.IsEnabled = true;
        }

        [Fact]
        public void EmitsOnToolkitStartup()
        {
            _toolkitContextFixture.TelemetryFixture.GetMetricsByMetricName(_codeWhispererEnabledMetricName).Should().BeEmpty();

            _toolkitContextFixture.ToolkitContextProvider.RaiseInitialized();
            var enabledMetrics = _toolkitContextFixture.TelemetryFixture.GetMetricsByMetricName(_codeWhispererEnabledMetricName)
                .SelectMany(x => x.Data);

            enabledMetrics.Should().HaveCount(1);
            var metric = enabledMetrics.Single();
            AssertCodeWhispererEnabledMetadata(metric, true, "startup");
        }

        [Fact]
        public void EmitsOnSettingsChange()
        {
            _toolkitContextFixture.ToolkitContextProvider.RaiseInitialized();
            _toolkitContextFixture.TelemetryFixture.LoggedMetrics.Clear();

            _settingsRepository.Settings.IsEnabled = false;
            _settingsRepository.RaiseSettingsSaved();

            var enabledMetrics = _toolkitContextFixture.TelemetryFixture.GetMetricsByMetricName(_codeWhispererEnabledMetricName)
                .SelectMany(x => x.Data);

            enabledMetrics.Should().HaveCount(1);
            var metric = enabledMetrics.Single();
            AssertCodeWhispererEnabledMetadata(metric, false, "settings");
        }

        [Fact]
        public void DoesNotEmitWhenSettingsUnchanged()
        {
            _toolkitContextFixture.ToolkitContextProvider.RaiseInitialized();
            _toolkitContextFixture.TelemetryFixture.LoggedMetrics.Clear();

            // Don't change the "Enabled" value, emulating "some other value" getting changed and saved.
            _settingsRepository.RaiseSettingsSaved();

            _toolkitContextFixture.TelemetryFixture.GetMetricsByMetricName(_codeWhispererEnabledMetricName).Should().BeEmpty();
        }

        private void AssertCodeWhispererEnabledMetadata(MetricDatum metric, bool isEnabled, string source)
        {
            metric.Metadata["enabled"].Should().Be(isEnabled.ToString().ToLower());
            metric.Metadata["source"].Should().Be(source);
        }

        public void Dispose()
        {
            _sut.Dispose();
        }
    }
}
