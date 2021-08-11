using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Tasks;

using log4net;

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
            try
            {
                var feedbackViewModel = new FeedbackPanelViewModel();
                var dialog = new FeedbackPanel(parameter as string) { DataContext = feedbackViewModel };

                var result = _toolkitContext.ToolkitHost.ShowInModalDialogWindow(dialog, MessageBoxButton.OKCancel);
                if (result)
                {
                    await feedbackViewModel.SubmitFeedbackAsync(_toolkitContext);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error launching feedback form", ex);
                _toolkitContext.ToolkitHost.ShowError("Failed to open the feedback form", ex.Message);

            }
        }
    }
}
