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
    public class ResumeCommandTests : IDisposable
    {
        private readonly FakeCodeWhispererManager _manager = new FakeCodeWhispererManager();
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly ResumeCommand _sut;
        private MetricDatum _metric = null;

        public ResumeCommandTests()
        {
            _sut = new ResumeCommand(_manager, _toolkitContextFixture.ToolkitContextProvider);
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
        [InlineData(true, true)]
        [InlineData(false, false)]
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
            _manager.PauseAutomaticSuggestions.Should().BeFalse();

            _metric.Metadata["settingId"].Should().BeEquivalentTo(CodeWhispererTelemetryConstants.AutoSuggestion.SettingId);
            _metric.Metadata["settingState"].Should().BeEquivalentTo(CodeWhispererTelemetryConstants.AutoSuggestion.Activated);
        }

        [Fact]
        public async Task ExecuteAsync_WhenInitiallyNotPaused()
        {
            SetManagerPausedState(false);

            await _sut.ExecuteAsync();
            _manager.PauseAutomaticSuggestions.Should().BeFalse();

            _metric.Should().BeNull();
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
