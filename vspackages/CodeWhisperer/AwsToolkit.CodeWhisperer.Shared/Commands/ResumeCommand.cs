using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Telemetry;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class ResumeCommand : BaseCommand, IDisposable
    {
        private readonly ICodeWhispererManager _manager;
        private bool _isPaused;

        public ResumeCommand(ICodeWhispererManager manager, IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
            _manager = manager;

            _manager.PauseAutoSuggestChanged += ManagerOnPauseAutoSuggestChanged;
            _isPaused = _manager.IsAutoSuggestPaused();
        }

        private void ManagerOnPauseAutoSuggestChanged(object sender, PauseStateChangedEventArgs e)
        {
            _isPaused = e.IsPaused;
        }

        protected override bool CanExecuteCore(object parameter)
        {
            return _isPaused && base.CanExecuteCore(parameter);
        }

        protected override async Task ExecuteCoreAsync(object parameter)
        {
            var result = new TaskResult();
            try
            {
                await _manager.ResumeAutoSuggestAsync();
                result.Status = TaskStatus.Success;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
            }

            RecordResume(result);
        }

        private void RecordResume(TaskResult result)
        {
            _toolkitContextProvider.GetToolkitContext().TelemetryLogger.RecordModifySetting(result,
                CodeWhispererTelemetryConstants.AutoSuggestion.SettingId, CodeWhispererTelemetryConstants.AutoSuggestion.Activated);
        }

        public void Dispose()
        {
            _manager.PauseAutoSuggestChanged -= ManagerOnPauseAutoSuggestChanged;
        }
    }
}
