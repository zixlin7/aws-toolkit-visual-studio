using Amazon.AWSToolkit.Models.Text;

using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Amazon.AwsToolkit.VsSdk.Common.Documents
{
    public static class TextViewExtensionMethods
    {
        public static IWpfTextView GetWpfTextView(this IVsTextView textView)
        {
            var userData = textView as IVsUserData;

            var guid = DefGuidList.guidIWpfTextViewHost;

            object wpfTextViewHost = null;

            userData?.GetData(ref guid, out wpfTextViewHost);

            return ((IWpfTextViewHost) wpfTextViewHost)?.TextView;
        }

        public static string GetFilePath(this IWpfTextView textView)
        {
            return textView.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDocument)
                ? textDocument.FilePath
                : null;
        }

        public static Position GetCursorPosition(this IWpfTextView textView)
        {
            var caretPosition = textView.Caret.Position.BufferPosition;

            var caretLine = caretPosition.GetContainingLine();

            var caretColumn = caretPosition.Position - caretLine.Start.Position;

            return new Position(caretLine.LineNumber, caretColumn);
        }

        /// <summary>
        /// Converts the given absolute position in a text view
        /// to the line and character index within the document.
        /// </summary>
        public static Position GetDocumentPosition(this IVsTextView textView, int position)
        {
            textView.GetBuffer(out var buffer);
            buffer.GetLineIndexOfPosition(position, out var line, out var column);
            return new Position(line, column);
        }

        /// <summary>
        /// Gets the text that exists between a start position and an end position within a VsTextView document.
        /// </summary>
        public static string GetTextBetweenPositions(this IVsTextView textView, int startPosition, int endPosition)
        {
            if (endPosition <= startPosition)
            {
                return string.Empty;
            }

            textView.GetBuffer(out var buffer);

            buffer.GetLineIndexOfPosition(startPosition, out var startLine, out var startColumn);

            buffer.GetLineIndexOfPosition(endPosition, out var endLine, out var endColumn);

            buffer.GetLineText(startLine, startColumn, endLine, endColumn, out var stringBuffer);
            return stringBuffer;
        }

        /// <summary>
        /// Gets the integer Snapshot Position of the text view's caret
        /// </summary>
        public static int GetCaretSnapshotPosition(this IWpfTextView textView)
        {
            return textView.Caret.Position.BufferPosition.Position;
        }
    }
}
