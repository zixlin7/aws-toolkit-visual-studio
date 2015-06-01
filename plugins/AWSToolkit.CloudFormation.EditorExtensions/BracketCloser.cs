using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    /// <summary>
    /// This class auto insert closing ", } and ] characters when the opening version is inserted.
    /// </summary>
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("CloudFormation Template Bracket Closer")]
    [ContentType(TemplateContentType.ContentType)]
    internal class BracketCloserControllerProvider : IIntellisenseControllerProvider
    {
        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new BracketCloserController(textView, this);
        }

        internal class BracketCloserController : IIntellisenseController
        {
            private ITextView _textView;
            private BracketCloserControllerProvider _componentContext;
            System.EventHandler<TextContentChangedEventArgs> _textChangeHandler;

            internal BracketCloserController(ITextView textView, BracketCloserControllerProvider componentContext)
            {
                // VS 2012 already adds the closing quotes and brackets so only activate
                // this extension if we are inside VS 2012
                if (ToolkitFactory.Instance.ShellProvider.ShellName != AWSToolkit.Constants.VS2010HostShell.ShellName)
                    return;

                _textView = textView;
                _componentContext = componentContext;
                _textChangeHandler = new System.EventHandler<TextContentChangedEventArgs>(TextBuffer_Changed);

                _textView.TextBuffer.PostChanged += new System.EventHandler(TextBuffer_PostChanged);
                _textView.TextBuffer.Changed += _textChangeHandler;
            }

            ITextChange _lastTextChange;
            string _closingChar;
            // Now that opening character has been committed insert the closing character
            void TextBuffer_PostChanged(object sender, System.EventArgs e)
            {
                ITextChange changeMade = this._lastTextChange;
                this._lastTextChange = null;

                if (changeMade != null)
                {
                    // Disable change listener so we don't get stuck in an infinite loop.
                    _textView.TextBuffer.Changed -= _textChangeHandler;
                    try
                    {
                        this._textView.TextBuffer.Insert(changeMade.NewSpan.End, this._closingChar);
                        this._textView.Caret.MoveTo(new SnapshotPoint(this._textView.TextSnapshot, changeMade.NewSpan.End));
                    }
                    finally
                    {
                        _textView.TextBuffer.Changed += _textChangeHandler;
                    }
                }
            }

            void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
            {
                if (e.Changes != null && e.Changes.Count == 1 && e.Changes[0].NewText != null)
                {
                    Func<string, char, bool> shouldAppend = (startChar, endChar) => (e.Changes[0].NewText.Trim() == startChar && (e.After.Length == e.Changes[0].NewPosition + 1 || e.After[e.Changes[0].NewPosition + 1] != endChar));

                    if (shouldAppend("{", '}'))
                    {
                        this._lastTextChange = e.Changes[0];
                        this._closingChar = "}";
                    }
                    else if (shouldAppend("[", ']'))
                    {
                        this._lastTextChange = e.Changes[0];
                        this._closingChar = "]";
                    }
                    // For quotes we need to see if the new quote balances the number of quotes on the line or not.  If not then we can go ahead and insert another one.
                    else if (e.Changes[0].NewText.Trim() == "\"")
                    {
                        var document = this._textView.TextBuffer.CurrentSnapshot.GetText();
                        int startPos = document.LastIndexOf('\n', e.Changes[0].NewPosition);
                        if (startPos == -1)
                            return;

                        int endPos = document.IndexOf('\n', e.Changes[0].NewPosition);
                        if (endPos == -1)
                            return;

                        string line = document.Substring(startPos, endPos - startPos);

                        int count = 0;
                        for (int i = 0; i < line.Length; i++)
                        {
                            // Handle the case of the quote being escaped
                            if (line[i] == '"' && (i == 0 || line[i - 1] != '\\'))
                            {
                                count++;
                            }
                        }

                        if (count % 2 == 1)
                        {
                            this._lastTextChange = e.Changes[0];
                            this._closingChar = "\"";
                        }
                    }
                }
            }

            #region IIntellisenseController Members

            public void ConnectSubjectBuffer(ITextBuffer subjectBuffer)
            {
            }

            public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer)
            {
            }

            public void Detach(ITextView textView)
            {
                if (_textView == textView)
                {
                    _textView.TextBuffer.Changed -= this.TextBuffer_Changed;
                    _textView = null;
                }
            }

            #endregion
        }
    }
}
