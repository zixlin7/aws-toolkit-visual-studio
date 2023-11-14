using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AwsToolkit.CodeWhisperer.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Commands
{
    public class PauseCommandTests : IDisposable
    {
        private readonly FakeCodeWhispererManager _manager = new FakeCodeWhispererManager();
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly PauseCommand _sut;
        private MetricDatum _metric = null;

        public PauseCommandTests()
        {
            _sut = new PauseCommand(_manager, _toolkitContextFixture.ToolkitContextProvider);
            _toolkitContextFixture.SetupTelemetryCallback(metrics =>
            {
                var datum = metrics.Data.FirstOrDefault(x => string.Equals(x.MetricName, "aws_modifySetting"));
                if (datum != null)
                {
                    _metric = datum;
                }
            });
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void CanExecute(bool initialPauseState, bool expectedCanExecute)
        {
            SetManagerPausedState(initialPauseState);

            _sut.CanExecute().Should().Be(expectedCanExecute);
        }

        [Fact]

        public async Task ExecuteAsync_WhenInitiallyPaused()
        {
            SetManagerPausedState(true);

            await _sut.ExecuteAsync();
            _manager.PauseAutomaticSuggestions.Should().BeTrue();

            _metric.Should().BeNull();
        }

        [Fact]
        public async Task ExecuteAsync_WhenInitiallyNotPaused()
        {
            SetManagerPausedState(false);

            await _sut.ExecuteAsync();
            _manager.PauseAutomaticSuggestions.Should().BeTrue();

            _metric.Metadata["settingId"].Should().BeEquivalentTo(CodeWhispererTelemetryConstants.AutoSuggestion.SettingId);
            _metric.Metadata["settingState"].Should().BeEquivalentTo(CodeWhispererTelemetryConstants.AutoSuggestion.Deactivated);
        }

        private void SetManagerPausedState(bool pauseState)
        {
            _manager.PauseAutomaticSuggestions = pauseState;
            _manager.RaisePauseAutoSuggestChanged();
        }

        public void Dispose()
        {
            _sut.Dispose();
        }
    }
}
