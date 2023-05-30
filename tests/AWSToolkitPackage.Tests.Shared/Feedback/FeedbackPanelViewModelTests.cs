using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Feedback;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.Feedback
{
    public class FeedbackPanelViewModelTests
    {

        public static IEnumerable<object[]> SentimentData = new List<object[]>
        {
            new object[] {true, Sentiment.Positive},
            new object[] {null, Sentiment.Negative},
            new object[] {false, Sentiment.Negative}
        };

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>();
        private readonly FeedbackPanelViewModel _sut;

        public FeedbackPanelViewModelTests()
        {
            _sut = new FeedbackPanelViewModel(_toolkitContextFixture.ToolkitContext);
        }

        [Fact]
        public void InitialRemainingCharacters()
        {
            Assert.Equal(2000, _sut.RemainingCharacters);

        }

        [Theory]
        [InlineData("", 2000)]
        [InlineData("good", 1996)]
        [InlineData("1", 1999)]
        public void UpdateRemainingCharacters(string comment, int expectedLimit)
        {
            _sut.FeedbackComment = comment;

            _sut.UpdateRemainingCharacters();

            Assert.Equal(expectedLimit, _sut.RemainingCharacters);
        }

        [Theory]
        [MemberData(nameof(SentimentData))]
        public async Task SubmitFeedback_Success(bool sentiment, Sentiment expectedSentiment)
        {
            _sut.FeedbackSentiment = sentiment;

            var result  = await _sut.SubmitFeedbackAsync(null);

            Assert.Equal(Result.Succeeded, result);
            AssertTelemetryFeedbackCall(expectedSentiment, null, null);
            _toolkitContextFixture.ToolkitHost.Verify(host => host.OutputToHostConsole(It.Is<string>(s => s.Contains("Thanks")), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task SubmitFeedback_Fail()
        {
            _sut.FeedbackSentiment = true;
            _toolkitContextFixture.TelemetryLogger
                .Setup(mock => mock.SendFeedback(It.IsAny<Sentiment>(), It.IsAny<string>(), _metadata)).Throws<Exception>();

            var result = await _sut.SubmitFeedbackAsync(null);

            Assert.Equal(Result.Failed, result);
            AssertTelemetryFeedbackCall(Sentiment.Positive, null, null);
            _toolkitContextFixture.ToolkitHost.Verify(host => host.ShowError(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SubmitFeedback_SendsSourceMarker()
        {
            _sut.FeedbackSentiment = true;
            _sut.FeedbackComment = "good";

            var result = await _sut.SubmitFeedbackAsync( "publish to beanstalk");

            Assert.Equal(Result.Succeeded, result);
            AssertTelemetryFeedbackCall(Sentiment.Positive, "good", "publish to beanstalk");
            _toolkitContextFixture.ToolkitHost.Verify(host => host.OutputToHostConsole(It.Is<string>(s => s.Contains("Thanks")), It.IsAny<bool>()), Times.Once);
        }

        private void AssertTelemetryFeedbackCall(Sentiment sentiment, string comment, string sourceMarker)
        {
            if (!string.IsNullOrWhiteSpace(sourceMarker))
            {
                _metadata.Add(FeedbackPanelViewModel.FeedbackSource, sourceMarker);
            }
            _toolkitContextFixture.TelemetryLogger.Verify(mock => mock.SendFeedback(sentiment, comment, _metadata), Times.Once);
        }
    }
}
