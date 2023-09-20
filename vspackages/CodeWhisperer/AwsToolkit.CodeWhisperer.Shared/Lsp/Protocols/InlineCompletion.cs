using Microsoft.VisualStudio.LanguageServer.Protocol;

using Newtonsoft.Json;

// This file contains LSP protocol messages relating to inline completions.
// Inline completion support is defined in the "Upcoming" lsp protocol spec:
// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/
// Many of the base protocol types are already defined in Microsoft.VisualStudio.LanguageServer.Protocol
namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols
{
    /// <summary>
    /// Requests InlineCompletions (code suggestions) from language server.
    /// </summary>
    public class InlineCompletionParams : TextDocumentPositionParams
    {
        /// <summary>
        /// Additional information about the context in which inline completions
        /// were requested.
        /// </summary>
        [JsonProperty("context")]
        public InlineCompletionContext Context { get; set; }
    }

    public class InlineCompletionContext
    {
        /// <summary>
        /// How the inline completion was triggered.
        /// </summary>
        [JsonProperty("triggerKind")]
        public InlineCompletionTriggerKind TriggerKind { get; set; }

        /// <summary>
        /// Optional
        /// 
        /// Provides information about the currently selected item in the
        /// autocomplete widget if it is visible.
        /// 
        /// If set, provided inline completions must extend the text of the
        /// selected item and use the same range, otherwise they are not shown as
        /// preview.
        /// As an example, if the document text is `console.` and the selected item
        /// is `.log` replacing the `.` in the document, the inline completion must
        /// also replace `.` and start with `.log`, for example `.log()`.
        /// 
        /// Inline completion providers are requested again whenever the selected
        /// item changes.
        /// </summary>
        [JsonProperty("selectedCompletionInfo")]
        public SelectedCompletionInfo SelectedCompletionInfo { get; set; }
    }

    public enum InlineCompletionTriggerKind
    {
        /// <summary>
        /// Completion was triggered explicitly by a user gesture.
        /// Return multiple completion items to enable cycling through them.
        /// </summary>
        Invoke = 0,
        /// <summary>
        /// Completion was triggered automatically while editing.
        /// It is sufficient to return a single completion item in this case.
        /// </summary>
        Automatic = 1,
    }

    public class SelectedCompletionInfo
    {
        /// <summary>
        /// The range that will be replaced if this completion item is accepted.
        /// These are zero-indexed values.
        /// </summary>
        [JsonProperty("range")]
        public Range Range { get; set; }

        /// <summary>
        /// The text the range will be replaced with if this completion is accepted.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    /// <summary>
    /// Response containing inline completions (code suggestions) from the language server
    /// </summary>
    public class InlineCompletionList
    {
        /// <summary>
        /// The inline completion items.
        /// </summary>
        public InlineCompletionItem[] Items { get; set; }
    }

    /// <summary>
    /// A text snippet that is proposed inline to complete text that is being typed.
    /// </summary>
    public class InlineCompletionItem
    {
        /// <summary>
        /// The text to replace the range with. Must be set.
        /// Is used both for the preview and the accept operation.
        /// </summary>
        public string InsertText { get; set; }

        /// <summary>
        /// Optional
        /// 
        /// A text that is used to decide if this inline completion should be
        /// shown. When `falsy` the {@link InlineCompletionItem.insertText} is
        /// used.
        /// 
        /// An inline completion is shown if the text to replace is a prefix of the
        /// filter text.
        /// </summary>
        public string FilterText { get; set; }

        /// <summary>
        /// Optional
        /// 
        /// The range to replace.
        /// Must begin and end on the same line.
        /// 
        /// Prefer replacements over insertions to provide a better experience when
        /// the user deletes typed text.
        /// </summary>
        public Range Range { get; set; }

        // There is a "command" field that was omitted. Add if we start to support it in the Toolkit.

        /// <summary>
        /// The format of the insert text. The format applies to the `insertText`.
        /// If omitted defaults to `InsertTextFormat.PlainText`.
        /// </summary>
        public InsertTextFormat InsertTextFormat { get; set; } = InsertTextFormat.PlainText;
    }

    public enum InsertTextFormat
    {
        /// <summary>
        /// The primary text to be inserted is treated as a plain string.
        /// </summary>
        PlainText = 1,

        /// <summary>
        /// The primary text to be inserted is treated as a snippet.
        /// 
        /// A snippet can define tab stops and placeholders with `$1`, `$2`
        /// and `${3:foo}`. `$0` defines the final tab stop, it defaults to
        /// the end of the snippet. Placeholders with equal identifiers are linked,
        /// that is typing in one will update others too.
        /// </summary>
        Snippet = 2,
    }
}
