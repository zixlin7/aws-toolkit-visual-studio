using System;
using System.Windows;

using Amazon.AWSToolkit.Feedback;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.Feedback
{
    public class SendFeedbackCommandTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly SendFeedbackCommand _sut;

        public SendFeedbackCommandTests()
        {
            _sut = new SendFeedbackCommand(_toolkitContextFixture.ToolkitContext);
        }

        [Fact]
        public void Execute_Submit()
        {
            _toolkitContextFixture.ToolkitHost.Setup(x =>
                x.ShowInModalDialogWindow(It.IsAny<IAWSToolkitControl>(), It.IsAny<MessageBoxButton>())).Returns(true);
            _sut.Execute(null);

            _toolkitContextFixture.ToolkitHost.Verify(x => x.OutputToHostConsole(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Execute_Cancel()
        {
            _toolkitContextFixture.ToolkitHost.Setup(x =>
                x.ShowInModalDialogWindow(It.IsAny<IAWSToolkitControl>(), It.IsAny<MessageBoxButton>())).Returns(false);
            _sut.Execute(null);

            _toolkitContextFixture.ToolkitHost.Verify(x => x.OutputToHostConsole(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Execute_Throws()
        {
            _toolkitContextFixture.ToolkitHost.Setup(x =>
                    x.ShowInModalDialogWindow(It.IsAny<IAWSToolkitControl>(), It.IsAny<MessageBoxButton>()))
                .Throws<Exception>();
            _sut.Execute(null);

            _toolkitContextFixture.ToolkitHost.Verify(x => x.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
