using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Telemetry;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class PauseCommand : BaseCommand, IDisposable
    {
        private readonly ICodeWhispererManager _manager;
        private bool _isPaused;

        public PauseCommand(ICodeWhispererManager manager, IToolkitContextProvider toolkitContextProvider)
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
            return
                base.CanExecuteCore(parameter)
                && !_isPaused
                && _manager.ConnectionStatus == ConnectionStatus.Connected;
        }


        protected override async Task ExecuteCoreAsync(object parameter)
        {
            var result = new TaskResult();
            try
            {
                await _manager.PauseAutoSuggestAsync();
                result.Status = TaskStatus.Success;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
            }
          
            RecordPause(result);
        }

        private void RecordPause(TaskResult result)
        {
            _toolkitContextProvider.GetToolkitContext().TelemetryLogger.RecordModifySetting(result,
                CodeWhispererTelemetryConstants.AutoSuggestion.SettingId, CodeWhispererTelemetryConstants.AutoSuggestion.Deactivated);
        }

        public void Dispose()
        {
            _manager.PauseAutoSuggestChanged -= ManagerOnPauseAutoSuggestChanged;
        }
    }
}
