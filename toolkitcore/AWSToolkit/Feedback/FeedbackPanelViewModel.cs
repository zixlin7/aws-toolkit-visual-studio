using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using System.Windows.Input;

namespace Amazon.AWSToolkit.Feedback
{
    /// <summary>
    /// Backing data for the toolkit feedback form
    /// </summary>
    public class FeedbackPanelViewModel : BaseModel
    {
        private readonly ToolkitContext _toolkitContext;
        public const string FeedbackSource = "source";
        public const int MAX_CHAR_LIMIT = 2000;
        private bool? _feedbackSentiment = null;
        private string _feedbackComment;
        private int _remainingCharacters = MAX_CHAR_LIMIT;
        private readonly IDictionary<string, string> _metadata = new Dictionary<string, string>();
        private ICommand _viewUrlCommand;
        private ICommand _viewUserGuideCommand;
       
        public FeedbackPanelViewModel(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

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

        public ICommand ViewUrlCommand
        {
            get => _viewUrlCommand;
            set => SetProperty(ref _viewUrlCommand, value, () => ViewUrlCommand);
        }

        public ICommand ViewUserGuideCommand
        {
            get => _viewUserGuideCommand;
            set => SetProperty(ref _viewUserGuideCommand, value, () => ViewUserGuideCommand);
        }

        public void UpdateRemainingCharacters()
        {
            RemainingCharacters = MAX_CHAR_LIMIT - FeedbackComment.Length;
        }

        public bool IsFeedbackCommentAboveLimit()
        {
            return FeedbackComment != null  && FeedbackComment.Length > MAX_CHAR_LIMIT;
        }

        public async Task<Result> SubmitFeedbackAsync(string sourceMarker)
        {
            try
            {
                AddSourceMetadata(sourceMarker);
                await _toolkitContext.TelemetryLogger.SendFeedback(GetSentiment(), FeedbackComment, _metadata);

                _toolkitContext.ToolkitHost.OutputToHostConsole("Thanks for the feedback!", true);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                _toolkitContext.ToolkitHost.ShowError($"Failed to submit {FeedbackSentiment} feedback: {ex.Message}");
                return Result.Failed;
            }
        }

        private void AddSourceMetadata(string sourceMarker)
        {
            if (!string.IsNullOrWhiteSpace(sourceMarker))
            {
                _metadata[FeedbackSource] = sourceMarker;
            }
        }

        private Sentiment GetSentiment()
        {
            return FeedbackSentiment == true ? Sentiment.Positive : Sentiment.Negative;
        }
    }
}
