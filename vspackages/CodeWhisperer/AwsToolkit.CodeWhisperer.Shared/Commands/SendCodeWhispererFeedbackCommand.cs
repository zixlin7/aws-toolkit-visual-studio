using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Feedback;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    /// <summary>
    /// This command wraps <see cref="SendFeedbackCommand"/> so that
    /// the command can be provided with ToolkitContext once it is
    /// available.
    /// </summary>
    public class SendCodeWhispererFeedbackCommand : BaseCommand
    {
        private SendFeedbackCommand _sendFeedbackCommand;

        public SendCodeWhispererFeedbackCommand(IToolkitContextProvider toolkitContextProvider) : base(toolkitContextProvider)
        {
        }

        protected override bool CanExecuteCore(object parameter)
        {
            if (!base.CanExecuteCore(parameter))
            {
                return false;
            }

            if (_sendFeedbackCommand == null)
            {
                _sendFeedbackCommand = new SendFeedbackCommand(_toolkitContextProvider.GetToolkitContext());
            }

            return _sendFeedbackCommand.CanExecute(parameter);
        }

        protected override Task ExecuteCoreAsync(object parameter)
        {
            _sendFeedbackCommand?.Execute(parameter);
            return Task.CompletedTask;
        }
    }
}
