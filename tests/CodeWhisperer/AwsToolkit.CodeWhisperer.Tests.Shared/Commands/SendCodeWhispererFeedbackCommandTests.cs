using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Commands
{
    public class SendCodeWhispererFeedbackCommandTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly SendCodeWhispererFeedbackCommand _sut;

        public SendCodeWhispererFeedbackCommandTests()
        {
            _sut = new SendCodeWhispererFeedbackCommand(_toolkitContextFixture.ToolkitContextProvider);
        }

        [Fact]
        public void CanExecute_NoToolkitContext()
        {
            _toolkitContextFixture.ToolkitContextProvider.HaveToolkitContext = false;
            _sut.CanExecute().Should().BeFalse();
        }

        [Fact]
        public void CanExecute()
        {
            _sut.CanExecute().Should().BeTrue();
        }
    }
}
