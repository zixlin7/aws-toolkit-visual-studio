using System.Threading.Tasks;

using Amazon.AWSToolkit.Feedback;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.Feedback
{
    public class FeedbackPanelViewModelTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly FeedbackPanelViewModel _sut;

        public FeedbackPanelViewModelTests()
        {
            _sut = new FeedbackPanelViewModel();
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

        [Fact]
        public async Task SubmitFeedback()
        {
            await _sut.SubmitFeedbackAsync(_toolkitContextFixture.ToolkitContext);

            _toolkitContextFixture.ToolkitHost.Verify(host => host.OutputToHostConsole(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }
    }
}
