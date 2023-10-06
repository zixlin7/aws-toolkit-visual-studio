using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
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

        public PauseCommandTests()
        {
            _sut = new PauseCommand(_manager, _toolkitContextFixture.ToolkitContextProvider);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void CanExecute(bool initialPauseState, bool expectedCanExecute)
        {
            SetManagerPausedState(initialPauseState);

            _sut.CanExecute().Should().Be(expectedCanExecute);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExecuteAsync(bool initialPauseState)
        {
            SetManagerPausedState(initialPauseState);

            await _sut.ExecuteAsync();
            _manager.PauseAutomaticSuggestions.Should().BeTrue();
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
