using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Feedback;

namespace Amazon.AWSToolkit.Commands
{
    /// <summary>
    /// Shows the "Send Feedback" dialog, watermarked for Arm Preview
    /// </summary>
    public class ShareArmPreviewFeedbackCommand : AsyncCommand
    {
        public const string Title = "Share feedback";
        private const string _feedbackWatermark = "Arm Preview";

        private readonly SendFeedbackCommand _command;

        public ShareArmPreviewFeedbackCommand(ToolkitContext toolkitContext)
        {
            _command = new SendFeedbackCommand(toolkitContext);
        }

        protected override Task ExecuteCoreAsync(object _)
        {
            _command.Execute(_feedbackWatermark);

            return Task.CompletedTask;
        }
    }
}
