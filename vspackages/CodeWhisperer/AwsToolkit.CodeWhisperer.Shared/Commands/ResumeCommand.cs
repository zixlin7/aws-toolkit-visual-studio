using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AWSToolkit.Context;

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
            await _manager.ResumeAutoSuggestAsync();
        }

        public void Dispose()
        {
            _manager.PauseAutoSuggestChanged -= ManagerOnPauseAutoSuggestChanged;
        }
    }
}
