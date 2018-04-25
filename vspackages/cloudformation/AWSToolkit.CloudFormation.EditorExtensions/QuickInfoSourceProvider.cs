using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

using Amazon.AWSToolkit.CloudFormation.Parser;
using log4net;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [ContentType(TemplateContentType.ContentType)]
    [Name("CloudFormationTemplateQuickInfo")]
    class QuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new QuickInfoSource(textBuffer);
        }

        class QuickInfoSource : IQuickInfoSource
        {
            private ITextBuffer _buffer;
            private bool _disposed = false;


            public QuickInfoSource(ITextBuffer buffer)
            {
                _buffer = buffer;
            }

            public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
            {
                applicableToSpan = null;

                if (_disposed)
                    return;

                try
                {

                    var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(_buffer.CurrentSnapshot);
                    if (triggerPoint == null)
                        return;

                    // First look for cached parse results.
                    ParserResults parserResults = null;
                    if (this._buffer.Properties.ContainsProperty(EditorContants.LAST_TEXT_BUFFER_PARSE_RESULTS))
                        parserResults = this._buffer.Properties[EditorContants.LAST_TEXT_BUFFER_PARSE_RESULTS] as ParserResults;

                    if (parserResults == null)
                    {
                        var parser = new TemplateParser();
                        parserResults = parser.Parse(_buffer.CurrentSnapshot.GetText());
                    }

                    // TODO Switch to binary search
                    foreach (var token in parserResults.HighlightedTemplateTokens)
                    {
                        if (token.Postion <= triggerPoint.Position && triggerPoint.Position < (token.Postion + token.Length))
                        {
                            if (!string.IsNullOrEmpty(token.Decription))
                            {
                                applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(token.Postion, token.Length, SpanTrackingMode.EdgeExclusive);
                                quickInfoContent.Add(token.Decription);
                                return;
                            }

                            break;
                        }
                    }

                    applicableToSpan = null;
                }
                catch (Exception ex)
                {
                    LogManager.GetLogger(typeof(QuickInfoSource)).Error("Error with quick info.", ex);
                }
            }

            public void Dispose()
            {
                _disposed = true;
            }
        }
    }
}
