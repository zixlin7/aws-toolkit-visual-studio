using System;
using System.Linq;
using System.Windows;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
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
            VsImage.Initialize(new FakeMultiValueConverter());
        }

        [StaFact]
        public void Execute_SubmitSuccessful()
        {
            SetupHostModalDialog(true);

            _sut.Execute(null);

            AssertTelemetryFeedbackCall(Result.Succeeded);
        }

        [StaFact]
        public void Execute_SubmitFailed()
        {
            SetupHostModalDialog(true);
            _toolkitContextFixture.TelemetryLogger.Setup(x => x.SendFeedback(It.IsAny<Sentiment>(), It.IsAny<string>()))
                .Throws<Exception>();

            _sut.Execute(null);

            AssertTelemetryFeedbackCall(Result.Failed);
        }

        [StaFact]
        public void Execute_Cancel()
        {
            SetupHostModalDialog(false);

            _sut.Execute(null);

            AssertTelemetryFeedbackCall(Result.Cancelled);
        }

        [StaFact]
        public void Execute_Throws()
        {
            _toolkitContextFixture.ToolkitHost.Setup(x =>
                    x.ShowInModalDialogWindow(It.IsAny<IAWSToolkitControl>(), It.IsAny<MessageBoxButton>()))
                .Throws<Exception>();

            _sut.Execute(null);

            AssertTelemetryFeedbackCall(Result.Failed);

            _toolkitContextFixture.ToolkitHost.Verify(x => x.ShowError(It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        private void SetupHostModalDialog(bool returnValue)
        {
            _toolkitContextFixture.ToolkitHost.Setup(x =>
                    x.ShowInModalDialogWindow(It.IsAny<IAWSToolkitControl>(), It.IsAny<MessageBoxButton>()))
                .Returns(returnValue);
        }

        private void AssertTelemetryFeedbackCall(Result result)
        {
            _toolkitContextFixture.TelemetryLogger.Verify(
                mock => mock.Record(It.Is<Metrics>(m =>
                    m.Data.Any(p => p.Metadata.Values.Contains(result.ToString())))), Times.Once);
        }
    }
}
