using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using log4net;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    /// <summary>
    /// Adds the brace matching support when the view is created
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(TemplateContentType.ContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class BraceMatchingFactory : IWpfTextViewCreationListener
    {
        [Import(typeof(ITextMarkerProviderFactory))]
        ITextMarkerProviderFactory TextMakerProviderFactory { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            // Create the brace matching presenter
            textView.Properties.GetOrCreateSingletonProperty<BraceMatchingPresenter>(() =>
                new BraceMatchingPresenter(textView, TextMakerProviderFactory)
                );
        }
    }

    internal class BraceMatchingPresenter
    {
        private IWpfTextView textView;
        private ITextMarkerProviderFactory textMarkerProviderFactory;

        internal BraceMatchingPresenter(IWpfTextView textView, ITextMarkerProviderFactory textMarkerProviderFactory)
        {
            this.textView = textView;
            this.textView.Caret.PositionChanged += new EventHandler<CaretPositionChangedEventArgs>(Caret_PositionChanged);
            this.textMarkerProviderFactory = textMarkerProviderFactory;
        }

        private void Caret_PositionChanged(object source, CaretPositionChangedEventArgs e)
        {
            try
            {
                // update all adornments when caret position is changed
                RemoveAllAdornments(e.TextView.TextBuffer);
                AddAdornments(e);
            }
            catch(Exception ex)
            {
                LogManager.GetLogger(typeof(BraceMatchingPresenter)).Error("Error computing bracket matches.", ex);
            }
        }

        private void AddAdornments(CaretPositionChangedEventArgs e)
        {
            // Use the brace matchers to highlight bounds
            if (e.TextView.TextViewLines != null)
            {
                var snapshotPoint = e.NewPosition.BufferPosition;
                if (!SearchForBrackets(e.TextView.TextBuffer, snapshotPoint))
                {
                    SearchForBrackets(e.TextView.TextBuffer, snapshotPoint.Add(-1));
                }
            }
        }

        bool SearchForBrackets(ITextBuffer textBuffer, SnapshotPoint snapshotPoint)
        {
            if (snapshotPoint == null)
                return false;

            var text = snapshotPoint.Snapshot.GetText();

            if (text.Length <= snapshotPoint.Position || snapshotPoint.Position < 0)
                return false;

            char current = text[snapshotPoint.Position];
            if (current == '{' || current == '}' || current == '[' || current == ']')
            {
                HighlightBounds(textBuffer, new SnapshotSpan(snapshotPoint, 1));

                if (current == '{')
                {
                    SearchForClosingBracket(textBuffer, snapshotPoint.Add(1), 1, '{', '}');
                }
                else if (current == '[')
                {
                    SearchForClosingBracket(textBuffer, snapshotPoint.Add(1), 1, '[', ']');
                }
                else if (current == '}')
                {
                    SearchForClosingBracket(textBuffer, snapshotPoint.Add(-1), -1, '}', '{');
                }
                else if (current == ']')
                {
                    SearchForClosingBracket(textBuffer, snapshotPoint.Add(-1), -1, ']', '[');
                }

                return true;
            }

            return false;
        }

        void SearchForClosingBracket(ITextBuffer textBuffer, SnapshotPoint snapshotPoint, int increment, char anchorChar, char charToSearch)
        {
            var text = snapshotPoint.Snapshot.GetText();

            if (snapshotPoint == null)
                return;

            char current = text[snapshotPoint.Position];

            if (text.Length <= snapshotPoint.Position || snapshotPoint.Position < 0)
                return;

            int index = snapshotPoint.Position;
            var stack = new Stack<char>();

            while(index < text.Length && index >= 0)
            {
                if (text[index] == charToSearch)
                {
                    if (stack.Count == 0)
                    {
                        HighlightBounds(textBuffer, new SnapshotSpan(snapshotPoint.Add(index - snapshotPoint.Position), 1));
                        break;
                    }
                    else
                    {
                        stack.Pop();
                    }
                }
                else if (text[index] == anchorChar)
                {
                    stack.Push(anchorChar);
                }

                index = index + increment;
            }
        }


        private void RemoveAllAdornments(ITextBuffer buffer)
        {
            textMarkerProviderFactory.GetTextMarkerTagger(buffer).RemoveTagSpans(span => true);
        }

        private void HighlightBounds(ITextBuffer buffer, SnapshotSpan span)
        {
            textMarkerProviderFactory.GetTextMarkerTagger(buffer).CreateTagSpan(span.Snapshot.CreateTrackingSpan(span.Span, SpanTrackingMode.EdgeExclusive), new TextMarkerTag("bracehighlight"));
        }
    }

}
