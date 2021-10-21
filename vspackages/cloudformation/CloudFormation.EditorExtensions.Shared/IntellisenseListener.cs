using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using Amazon.AWSToolkit.CloudFormation.Parser.Schema;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(TemplateContentType.ContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class IntellisenseListener : IVsTextViewCreationListener
    {
        [Import]
        IVsEditorAdaptersFactoryService AdaptersFactory = null;

        [Import]
        ICompletionBroker CompletionBroker = null;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            Debug.Assert(view != null);

            CommandFilter filter = new CommandFilter(view, CompletionBroker);

            IOleCommandTarget next;
            textViewAdapter.AddCommandFilter(filter, out next);
            filter.Next = next;
        }

        internal sealed class CommandFilter : IOleCommandTarget
        {
            ICompletionSession _currentSession;

            public CommandFilter(IWpfTextView textView, ICompletionBroker broker)
            {
                _currentSession = null;

                TextView = textView;
                Broker = broker;
            }

            public IWpfTextView TextView { get; }
            public ICompletionBroker Broker { get; }
            public IOleCommandTarget Next { get; set; }

            private char GetTypeChar(IntPtr pvaIn)
            {
                return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }

            public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                bool handled = false;
                int hresult = VSConstants.S_OK;

                // 1. Pre-process
                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    switch ((VSConstants.VSStd2KCmdID)nCmdID)
                    {
                        case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                        case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                            handled = StartSession();
                            break;
                        case VSConstants.VSStd2KCmdID.RETURN:
                            handled = Complete(false);
                            break;
                        case VSConstants.VSStd2KCmdID.TAB:
                            handled = Complete(true);
                            break;
                        case VSConstants.VSStd2KCmdID.CANCEL:
                            handled = Cancel();
                            break;
                    }
                }

                if (!handled)
                    hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                if (ErrorHandler.Succeeded(hresult))
                {
                    if (pguidCmdGroup == VSConstants.VSStd2K)
                    {
                        switch ((VSConstants.VSStd2KCmdID)nCmdID)
                        {
                            case VSConstants.VSStd2KCmdID.TYPECHAR:
                                char ch = GetTypeChar(pvaIn);
                                if (ch == ' ')
                                    StartSession();
                                else if (_currentSession != null)
                                    Filter();
                                break;
                            case VSConstants.VSStd2KCmdID.BACKSPACE:
                                Filter();
                                break;
                        }
                    }
                }

                return hresult;
            }

            private void Filter()
            {
                if (_currentSession == null)
                    return;

                _currentSession.SelectedCompletionSet.SelectBestMatch();
                _currentSession.SelectedCompletionSet.Recalculate();
            }

            bool Cancel()
            {
                if (_currentSession == null)
                    return false;

                _currentSession.Dismiss();

                return true;
            }

            bool Complete(bool force)
            {
                if (_currentSession == null)
                    return false;

                if (!_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected && !force)
                {
                    _currentSession.Dismiss();
                    return false;
                }
                else
                {
                    _currentSession.Commit();

                    // If the new inserted value contains a schema which says the new property is either boolean or only allows a set of values then pop
                    // up intellisense again for the list of valid values.
                    if (this._lastSelectedCompletionStatus != null)
                    {
                        if (this._lastSelectedCompletionStatus.Completion is TemplateCompletion && ((TemplateCompletion)this._lastSelectedCompletionStatus.Completion).Schema != null)
                        {
                            var schema = ((TemplateCompletion)this._lastSelectedCompletionStatus.Completion).Schema;

                            if (schema.AllowedValuesCount > 0 || schema.SchemaType == SchemaType.Boolean || schema.SchemaType == SchemaType.Resource)
                            {
                                StartSession();
                            }
                        }
                    }

                    return true;
                }
            }

            bool StartSession()
            {
                if (this._currentSession != null)
                    return false;

                SnapshotPoint caret = TextView.Caret.Position.BufferPosition;

                // See if the caret is in front of virtual spaces which happens after creating a new line and the cursor
                // gets auto indented.
                int virtualSpaces = this.TextView.Caret.Position.VirtualSpaces;
                if (virtualSpaces > 0)
                {
                    this.TextView.TextBuffer.Insert(caret.Position, new string(' ', this.TextView.Caret.Position.VirtualSpaces));
                    this.TextView.Caret.MoveTo(new SnapshotPoint(this.TextView.TextSnapshot, caret.Position + virtualSpaces));

                    // Reset caret position now that we moved.
                    caret = TextView.Caret.Position.BufferPosition;
                }

                ITextSnapshot snapshot = caret.Snapshot;


                if (!Broker.IsCompletionActive(TextView))
                {
                    this._currentSession = Broker.CreateCompletionSession(TextView, snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive), true);
                }
                else
                {
                    this._currentSession = Broker.GetSessions(TextView)[0];
                }

                this._lastSelectedCompletionStatus = null;
                this._currentSession.Dismissed += (sender, args) => _currentSession = null;
                this._currentSession.Committed += new System.EventHandler(OnActiveSessionCommited);
                this._currentSession.SelectedCompletionSetChanged += new EventHandler<ValueChangedEventArgs<CompletionSet>>(_currentSession_SelectedCompletionSetChanged);
                this._currentSession.Start();

                return true;
            }


            CompletionSelectionStatus _lastSelectedCompletionStatus;
            void SelectedCompletionSet_SelectionStatusChanged(object sender, ValueChangedEventArgs<CompletionSelectionStatus> e)
            {
                if(e.NewValue.IsSelected)
                    this._lastSelectedCompletionStatus = e.NewValue;
            }

            void _currentSession_SelectedCompletionSetChanged(object sender, ValueChangedEventArgs<CompletionSet> e)
            {
                this._currentSession.SelectedCompletionSet.SelectionStatusChanged += new EventHandler<ValueChangedEventArgs<CompletionSelectionStatus>>(SelectedCompletionSet_SelectionStatusChanged);
            }

            private void OnActiveSessionCommited(object sender, System.EventArgs e)
            {
                var span = this._currentSession.SelectedCompletionSet.ApplicableTo.GetSpan(this.TextView.TextSnapshot);
                var text = this.TextView.TextSnapshot.GetText(span);

                int posFirstEmptyString = text.IndexOf("\"\"");
                if (posFirstEmptyString != -1)
                {
                    this.TextView.Caret.MoveTo(new SnapshotPoint(this.TextView.TextSnapshot, span.Start + posFirstEmptyString + 1));
                }
                else if (text.EndsWith("\"\"") || text.EndsWith("{}") || text.EndsWith("[]"))
                {
                    this.TextView.Caret.MoveTo(new SnapshotPoint(this.TextView.TextSnapshot, span.End - 1));
                }

                this._currentSession = null;
            }

            public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            {
                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
                    {
                        case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                        case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                            prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                            return VSConstants.S_OK;
                    }
                }
                return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }
        }

    }
}
