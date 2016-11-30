using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Amazon.AWSToolkit.CloudFormation.Parser;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    /// <summary>
    /// https://blogs.msdn.microsoft.com/mshneer/2009/12/07/vs-2010-compiler-error-interop-type-xxx-cannot-be-embedded-use-the-applicable-interface-instead/
    /// </summary>
    internal class EnvDTEConstants
    {
        public const string vsViewKindCode = "{7651A701-06E5-11D1-8EBD-00A0C90F26EA}";
    }

    internal class ErrorTagger : ITagger<IErrorTag>
    {

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ErrorTagger));

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        ITextDocument _document;
        ITextBuffer _buffer;

        object updateLock = new object();
        List<ErrorToken> CurrentErrorTokens
        { get; set; }
        ITextSnapshot CurrentSnapshot { get; set; }
        ITextSnapshot RequestSnapshot { get; set; }

        Guid _lastKickOff;
        string _filePath;


        public ErrorTagger(ITextBuffer buffer)
        {
            this._buffer = buffer;
            _buffer.Changed += BufferChanged;

            if (this._buffer.Properties.ContainsProperty(typeof(ITextDocument)))
            {
                this._document = this._buffer.Properties.GetProperty<ITextDocument>(typeof(ITextDocument));
            }
            if (this._buffer.Properties.ContainsProperty(EditorContants.FILE_PATH_PROPERTY_NAME))
            {
                this._filePath = this._buffer.Properties.GetProperty<string>(EditorContants.FILE_PATH_PROPERTY_NAME);
            }

            // Do initial scan of spans
            var args = new TextContentChangedEventArgs(_buffer.CurrentSnapshot, _buffer.CurrentSnapshot, EditOptions.None, null);
            BufferChanged(this, args);
        }

        void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (CurrentSnapshot != null && CurrentSnapshot == e.After)
            {
                return;
            }

            RequestSnapshot = e.After;
            this._lastKickOff = Guid.NewGuid();
            ThreadPool.QueueUserWorkItem(UpdateAdornments, this._lastKickOff);
        }

        void UpdateAdornments(object threadContext)
        {
            try
            {
                // Sleep for a little bit to see if more changes are coming.
                Thread.Sleep(500);
                if ((Guid)threadContext != this._lastKickOff)
                    return;

                var currentTextSnapshot = RequestSnapshot;

                var errorTokens = new List<ErrorToken>();
                var parseErrorToken = JSONValidatorWrapper.Validate(currentTextSnapshot.GetText());
                if (parseErrorToken != null)
                    errorTokens.Add(parseErrorToken);

                ParserResults parserResults = null;
                if (this._buffer.Properties.ContainsProperty(EditorContants.LAST_TEXT_BUFFER_PARSE_RESULTS))
                {
                    parserResults = this._buffer.Properties[EditorContants.LAST_TEXT_BUFFER_PARSE_RESULTS] as ParserResults;

                    foreach (var token in parserResults.HighlightedTemplateTokens)
                    {
                        switch(token.Type)
                        {
                            case TemplateTokenType.InvalidKey:
                            case TemplateTokenType.DuplicateKey:
                            case TemplateTokenType.InvalidTypeReference:
                            case TemplateTokenType.NotAllowedValue:
                            case TemplateTokenType.UnknownMapKey:
                            case TemplateTokenType.UnknownReference:
                            case TemplateTokenType.UnknownResource:
                            case TemplateTokenType.UnknownResourceAttribute:
                                var errorToken = new ErrorToken(ErrorTokenType.SchemaValidation, token.Decription, false, token.Postion, token.Postion + token.Length);
                                errorTokens.Add(errorToken);
                                break;
                            default:
                                continue;
                        }
                    }
                }

                


                // If we are still up-to-date (another change hasn't happened yet), do a real update
                if (currentTextSnapshot == RequestSnapshot)
                    SynchronousUpdate(currentTextSnapshot, errorTokens);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error processing document for error tokens", e);
            }
        }

        /// <summary>
        /// Perform a synchronous update, in case multiple background threads are running
        /// </summary>
        void SynchronousUpdate(ITextSnapshot currentTextSnapshot, List<ErrorToken> errorTokens)
        {
            lock (updateLock)
            {
                if (currentTextSnapshot != RequestSnapshot)
                    return;

                CurrentErrorTokens = errorTokens;
                CurrentSnapshot = currentTextSnapshot;

                var tempEvent = TagsChanged;
                if (tempEvent != null)
                    tempEvent(this, new SnapshotSpanEventArgs(new SnapshotSpan(CurrentSnapshot, 0, CurrentSnapshot.Length)));
            }
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            IErrorListReporter errorListReporter = null;
            if (this._buffer.Properties.ContainsProperty(typeof(IErrorListReporter)))
            {
                errorListReporter = this._buffer.Properties.GetProperty<IErrorListReporter>(typeof(IErrorListReporter));
            }

            try
            {
                if (errorListReporter != null)
                {
                    errorListReporter.SuspendRefresh();
                    ClearErrors(errorListReporter);
                }

                var currentSpan = this.CurrentSnapshot;
                var currentErrorTokens = this.CurrentErrorTokens;
                if (currentErrorTokens == null || currentErrorTokens.Count == 0)
                    yield break;

                if (spans == null || spans.Count == 0 || currentSpan != spans[0].Snapshot)
                    yield break;

                foreach (var errorToken in currentErrorTokens)
                {
                    if (errorListReporter != null)
                    {
                        Task newError = ConvertToErrorTask(errorToken, errorListReporter);
                        errorListReporter.Tasks.Add(newError);
                    }

                    var errorTag = new ErrorTag("syntax error", errorToken.ShowToolTip ? errorToken.Message : null);
                    Span span;
                    if (errorToken.LineNumber != -1)
                    {
                        var line = this._buffer.CurrentSnapshot.GetLineFromLineNumber(errorToken.LineNumber);
                        int start = line.Start + errorToken.LineStartOffset;
                        int end = errorToken.LineEndOffSet <= errorToken.LineStartOffset ? line.End : line.Start + errorToken.LineEndOffSet;
                        span = new Span(start, end - start);
                    }
                    else
                    {
                        span = new Span(errorToken.StartPos, errorToken.EndPos - errorToken.StartPos);
                    }

                    var snapshotSpan = new SnapshotSpan(this._buffer.CurrentSnapshot, span);
                    var tagSpan = new TagSpan<IErrorTag>(snapshotSpan, errorTag);
                    yield return tagSpan;
                }
            }
            finally
            {
                if (errorListReporter != null)
                    errorListReporter.ResumeRefresh();
            }
        }

        void ClearErrors(IErrorListReporter errorListReporter)
        { 
            if(string.IsNullOrEmpty(this._filePath))
                errorListReporter.Tasks.Clear();

            var errorsToRemove = new List<ErrorTask>();

            foreach (var task in errorListReporter.Tasks)
            {
                var errorTask = task as ErrorTask;
                if (errorTask == null)
                    continue;

                if (string.Equals(errorTask.Document, this._filePath, StringComparison.InvariantCultureIgnoreCase))
                    errorsToRemove.Add(errorTask);
            }

            foreach (var errortTask in errorsToRemove)
            {
                errorListReporter.Tasks.Remove(errortTask);
            }
        }


        ErrorTask ConvertToErrorTask(ErrorToken errorToken, IErrorListReporter errorListReporter)
        {
            ErrorTask newError = new ErrorTask();
            newError.Text = errorToken.Message;
            newError.Category = TaskCategory.BuildCompile;

            if(this._document != null)
                newError.Document = this._document.FilePath;

            newError.ErrorCategory = TaskErrorCategory.Error;

            int line = -1;
            int column = 0;
            if (errorToken.LineNumber != -1)
            {
                line = errorToken.LineNumber;
                column = errorToken.LineStartOffset;
            }
            else
            {
                var l = this._buffer.CurrentSnapshot.GetLineFromPosition(errorToken.StartPos);
                line = l.LineNumber;
                column = errorToken.StartPos - l.Start.Position;
            }

            if (line != -1)
            {
                newError.Line = line;
                newError.Column = column;
            }

            newError.Navigate += new EventHandler((o, e) => 
                {
                    var task = o as ErrorTask;
                    try
                    {
                        ErrorTask copyTask = new ErrorTask();

                        copyTask.Document = task.Document;
                        copyTask.Line = task.Line + 1;
                        copyTask.Column = task.Column;

                        errorListReporter.Navigate(copyTask, Guid.Parse(EnvDTEConstants.vsViewKindCode));
                    }
                    catch { }
                });

            return newError;
        }
    }



    [Export(typeof(ITaggerProvider))]
    [ContentType(TemplateContentType.ContentType)]
    [TagType(typeof(IErrorTag))]
    internal class TestTaggerProvider : ITaggerProvider
    {

        [Import(typeof(SVsServiceProvider))]
        private IServiceProvider ServiceProvider { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new ErrorTagger(buffer) as ITagger<T>;
        }
    }
}
