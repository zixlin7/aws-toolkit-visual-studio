using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Models.Text;
using Amazon.AwsToolkit.VsSdk.Common.Documents;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class GetSuggestionsCommand : BaseCommand
    {
        private readonly IWpfTextView _textView;
        private readonly ICodeWhispererManager _manager;
        private readonly ISuggestionUiManager _suggestionUiManager;

        public GetSuggestionsCommand(IWpfTextView textView, ICodeWhispererManager manager,
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
            return base.CanExecuteCore(parameter) && _textView.Caret != null && !string.IsNullOrWhiteSpace(_textView.GetFilePath());
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
                var request = CreateGetSuggestionsRequest();
                var suggestions = (await _manager.GetSuggestionsAsync(request)).ToList();
                await _suggestionUiManager.ShowAsync(suggestions, _textView);
            });
        }

        private GetSuggestionsRequest CreateGetSuggestionsRequest()
        {
            return new GetSuggestionsRequest()
            {
                FilePath = _textView.GetFilePath(),
                CursorPosition = _textView.GetCursorPosition(),
                IsAutoSuggestion = false,
            };
        }
    }
}
