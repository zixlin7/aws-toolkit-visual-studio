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
    }
}
