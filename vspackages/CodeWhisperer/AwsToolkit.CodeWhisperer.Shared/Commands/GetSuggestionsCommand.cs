using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Models.Text;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class GetSuggestionsCommand : BaseCommand
    {
        private readonly IWpfTextView _textView;
        private readonly ICodeWhispererManager _manager;

        public GetSuggestionsCommand(IWpfTextView textView, ICodeWhispererManager manager,
            IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
            _textView = textView;
            _manager = manager;
        }

        protected override bool CanExecuteCore(object parameter)
        {
            return base.CanExecuteCore(parameter) && _textView.Caret != null && !string.IsNullOrWhiteSpace(GetFilePath());
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

                // TODO : IDE-11521 : display suggestions to user
                // Until then, output the suggestions in the Output pane

                toolkitHost.OutputToHostConsole($"---------- Suggestions: {suggestions.Count} ----------");

                foreach (var suggestion in suggestions)
                {
                    toolkitHost.OutputToHostConsole(suggestion.Text);
                    toolkitHost.OutputToHostConsole("------------------------------");
                }
            });
        }

        private GetSuggestionsRequest CreateGetSuggestionsRequest()
        {
            var caretPosition = _textView.Caret.Position.BufferPosition;
            var caretLine = caretPosition.GetContainingLine();
            var caretColumn = caretPosition.Position - caretLine.Start.Position;

            return new GetSuggestionsRequest()
            {
                FilePath = GetFilePath(),
                CursorPosition = new Position(caretLine.LineNumber, caretColumn),
                IsAutoSuggestion = false,
            };
        }

        private string GetFilePath()
        {
            return _textView.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDocument)
                ? textDocument.FilePath
                : null;
        }
    }
}
