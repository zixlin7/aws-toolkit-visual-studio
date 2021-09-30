using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.Feedback
{
    /// <summary>
    /// Backing data for the toolkit feedback form
    /// </summary>
    public class FeedbackPanelViewModel : BaseModel
    {
        public const int MAX_CHAR_LIMIT = 2000;
        private bool? _feedbackSentiment = null;
        private string _feedbackComment;
        private int _remainingCharacters = MAX_CHAR_LIMIT;

        public bool? FeedbackSentiment
        {
            get => _feedbackSentiment;
            set => SetProperty(ref _feedbackSentiment, value);
        }

        public string FeedbackComment
        {
            get => _feedbackComment;
            set => SetProperty(ref _feedbackComment, value);
        }

        public int RemainingCharacters
        {
            get => _remainingCharacters;
            set => SetProperty(ref _remainingCharacters, value);
        }

        public void UpdateRemainingCharacters()
        {
            RemainingCharacters = MAX_CHAR_LIMIT - FeedbackComment.Length;
        }

        public bool IsFeedbackCommentAboveLimit()
        {
            return FeedbackComment != null  && FeedbackComment.Length > MAX_CHAR_LIMIT;
        }

        public async Task<Result> SubmitFeedbackAsync(ToolkitContext toolkitContext, string sourceMarker)
        {
            try
            {
                await toolkitContext.TelemetryLogger.SendFeedback(GetSentiment(), CreateFeedbackComment(sourceMarker));

                toolkitContext.ToolkitHost.OutputToHostConsole("Thanks for the feedback!", true);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                toolkitContext.ToolkitHost.ShowError($"Failed to submit {FeedbackSentiment} feedback: {ex.Message}");
                return Result.Failed;
            }
        }

        private string CreateFeedbackComment(string marker)
        {
            if (!string.IsNullOrWhiteSpace(marker))
            {
                return $"System: {marker}{Environment.NewLine}{FeedbackComment}";
            }

            return FeedbackComment;
        }

        private Sentiment GetSentiment()
        {
            return FeedbackSentiment == true ? Sentiment.Positive : Sentiment.Negative;
        }
    }
}
