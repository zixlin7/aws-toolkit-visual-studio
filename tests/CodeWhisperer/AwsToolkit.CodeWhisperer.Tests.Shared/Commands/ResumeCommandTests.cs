using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Credentials;
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
        [InlineData(false, ConnectionStatus.Connected, true)]
        [InlineData(false, ConnectionStatus.Disconnected, false)]
        [InlineData(false, ConnectionStatus.Expired, false)]
        [InlineData(true, ConnectionStatus.Connected, false)]
        [InlineData(true, ConnectionStatus.Disconnected, false)]
        [InlineData(true, ConnectionStatus.Expired, false)]
        public void CanExecute(bool isAutoSuggestEnabledState, ConnectionStatus connectionStatus, bool expectedCanExecute)
        {
            _manager.ConnectionStatus = connectionStatus;
            SetManagerAutoSuggestionsEnabledState(isAutoSuggestEnabledState);

            _sut.CanExecute().Should().Be(expectedCanExecute);
        }


        [Fact]

        public async Task ExecuteAsync_WhenInitiallyPaused()
        {
            _manager.ConnectionStatus = ConnectionStatus.Connected;
            SetManagerAutoSuggestionsEnabledState(false);

            await _sut.ExecuteAsync();
            _manager.AutomaticSuggestionsEnabled.Should().BeTrue();

            _metric.Metadata["settingId"].Should().BeEquivalentTo(CodeWhispererTelemetryConstants.AutoSuggestion.SettingId);
            _metric.Metadata["settingState"].Should().BeEquivalentTo(CodeWhispererTelemetryConstants.AutoSuggestion.Activated);
        }

        [Fact]
        public async Task ExecuteAsync_WhenInitiallyNotPaused()
        {
            _manager.ConnectionStatus = ConnectionStatus.Connected;
            SetManagerAutoSuggestionsEnabledState(true);

            await _sut.ExecuteAsync();
            _manager.AutomaticSuggestionsEnabled.Should().BeTrue();

            _metric.Should().BeNull();
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
