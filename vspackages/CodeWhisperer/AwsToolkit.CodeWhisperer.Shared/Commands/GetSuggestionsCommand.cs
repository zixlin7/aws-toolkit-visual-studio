using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.VsSdk.Common.Documents;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class GetSuggestionsCommand : BaseCommand
    {
        private readonly ICodeWhispererManager _manager;
        private readonly ISuggestionUiManager _suggestionUiManager;
        private readonly ICodeWhispererTextView _textView;

        public GetSuggestionsCommand(
            ICodeWhispererTextView textView,
            ICodeWhispererManager manager,
            ISuggestionUiManager suggestionUiManager,
            IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
            _textView = textView;
            _manager = manager;
            _suggestionUiManager = suggestionUiManager;
        }

        protected override bool CanExecuteCore(object parameter)
        {
            return base.CanExecuteCore(parameter)
                   && _manager.ClientStatus == LspClientStatus.Running
                   && _manager.ConnectionStatus == ConnectionStatus.Connected
                   && _textView.GetWpfTextView().Caret != null
                   && !string.IsNullOrWhiteSpace(_textView.GetFilePath());
        }

        protected override async Task ExecuteCoreAsync(object parameter)
        {
            var toolkitHost = _toolkitContextProvider.GetToolkitContext().ToolkitHost;

            // TODO : IDE-11363 : using a task notifier is temporary. It helps during development to know that a query is running.
            var taskNotifier = await toolkitHost.CreateTaskStatusNotifier();
            taskNotifier.Title = "Retrieving CodeWhisperer suggestions";
            taskNotifier.CanCancel = false;
            taskNotifier.ShowTaskStatus(async _ =>
            {
                var request = _textView.CreateGetSuggestionsRequest(false);
                var requestPosition = _textView.GetWpfTextView().GetCaretSnapshotPosition();

                var suggestionSession = await _manager.GetSuggestionsAsync(request);

                // only if suggestion session is valid, attempt to load suggestions in UI
                if (suggestionSession.IsValid())
                {
                    var invocationProperties =
                        SuggestionUtilities.CreateInvocationProperties(request.IsAutoSuggestion, suggestionSession.SessionId, requestPosition, suggestionSession.RequestedAtEpoch);
                    await _suggestionUiManager.ShowAsync(suggestionSession.Suggestions, invocationProperties, _textView);
                }
            });
        }
    }
}
