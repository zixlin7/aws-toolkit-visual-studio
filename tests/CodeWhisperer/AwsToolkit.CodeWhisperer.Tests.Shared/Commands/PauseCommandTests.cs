using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
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
        private MetricDatum _metric;

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
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void CanExecute(bool isAutoSuggestEnabledState, bool expectedCanExecute)
        {
            SetManagerAutoSuggestionsEnabledState(isAutoSuggestEnabledState);

            _sut.CanExecute().Should().Be(expectedCanExecute);
        }

        [Fact]

        public async Task ExecuteAsync_WhenInitiallyPaused()
        {
            SetManagerAutoSuggestionsEnabledState(false);

            await _sut.ExecuteAsync();
            _manager.AutomaticSuggestionsEnabled.Should().BeFalse();

            _metric.Should().BeNull();
        }

        [Fact]
        public async Task ExecuteAsync_WhenInitiallyNotPaused()
        {
            SetManagerAutoSuggestionsEnabledState(true);

            await _sut.ExecuteAsync();
            _manager.AutomaticSuggestionsEnabled.Should().BeFalse();

            _metric.Metadata["settingId"].Should().BeEquivalentTo(CodeWhispererTelemetryConstants.AutoSuggestion.SettingId);
            _metric.Metadata["settingState"].Should().BeEquivalentTo(CodeWhispererTelemetryConstants.AutoSuggestion.Deactivated);
        }

        private void SetManagerAutoSuggestionsEnabledState(bool isAutoSuggestEnabledState)
        {
            _manager.AutomaticSuggestionsEnabled = isAutoSuggestEnabledState;
            _manager.RaisePauseAutoSuggestChanged();
        }

        public void Dispose()
        {
            _sut.Dispose();
        }
    }
}
