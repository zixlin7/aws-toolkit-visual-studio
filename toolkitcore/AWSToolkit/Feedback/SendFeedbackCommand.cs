using System;
using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Tasks;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using log4net;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.Feedback
{
    /// <summary>
    /// ICommand abstraction for showing Toolkit Feedback form to be used across the toolkit
    /// </summary>
    public class SendFeedbackCommand : ICommand
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(SendFeedbackCommand));
        public event EventHandler CanExecuteChanged;
        private readonly ToolkitContext _toolkitContext;

        public SendFeedbackCommand(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            ExecuteAsync(parameter).LogExceptionAndForget();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task ExecuteAsync(object parameter)
        {
            if (CanExecute(parameter))
            {
                await SubmitFeedbackAsync(parameter);
            }

            RaiseCanExecuteChanged();
        }

        private async Task SubmitFeedbackAsync(object parameter)
        {
            var feedbackResult = Result.Failed;
            try
            {
                var feedbackViewModel = new FeedbackPanelViewModel();
                var sourceMarker = parameter as string;

                var dialog = new FeedbackPanel(sourceMarker) {DataContext = feedbackViewModel};

                var result = _toolkitContext.ToolkitHost.ShowInModalDialogWindow(dialog, MessageBoxButton.OKCancel);
                if (result)
                {
                    feedbackResult = await feedbackViewModel.SubmitFeedbackAsync(_toolkitContext, sourceMarker);
                }
                else
                {
                    feedbackResult = Result.Cancelled;
                }
                
            }
            catch (Exception ex)
            {
                Logger.Error($"Error launching feedback form", ex);
                _toolkitContext.ToolkitHost.ShowError("Failed to open the feedback form", ex.Message);
            }
            finally
            {
                _toolkitContext.TelemetryLogger.RecordFeedbackResult(new FeedbackResult() {Result = feedbackResult});
            }
        }
    }
}
