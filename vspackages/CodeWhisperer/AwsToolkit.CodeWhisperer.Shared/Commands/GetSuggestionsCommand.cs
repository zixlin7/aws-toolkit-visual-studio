using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AWSToolkit.Context;

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
            var request = CreateGetSuggestionsRequest();
            var suggestions = await _manager.GetSuggestionsAsync(request);

            // TODO : IDE-11521 : display suggestions to user
        }

        private GetSuggestionsRequest CreateGetSuggestionsRequest()
        {
            var caretPosition = _textView.Caret.Position.BufferPosition;
            var caretLine = caretPosition.GetContainingLine();
            var caretColumn = caretPosition.Position - caretLine.Start.Position;

            return new GetSuggestionsRequest()
            {
                FilePath = GetFilePath(),
                CursorLine = caretLine.LineNumber,
                CursorColumn = caretColumn,
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
