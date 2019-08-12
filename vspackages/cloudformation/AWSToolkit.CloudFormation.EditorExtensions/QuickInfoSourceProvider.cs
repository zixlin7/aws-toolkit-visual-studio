using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

using Amazon.AWSToolkit.CloudFormation.Parser;
using log4net;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [ContentType(TemplateContentType.ContentType)]
    [Name("CloudFormationTemplateQuickInfo")]
    class QuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new QuickInfoSource(textBuffer);
        }

        class QuickInfoSource : IAsyncQuickInfoSource
        {
            private ITextBuffer _buffer;
            private bool _disposed = false;


            public QuickInfoSource(ITextBuffer buffer)
            {
                _buffer = buffer;
            }

            public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
            {
                return Task.Run<QuickInfoItem>(() =>
                {
                    if (_disposed)
                        return null;

                    try
                    {

                        var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(_buffer.CurrentSnapshot);
                        if (triggerPoint == null)
                            return null;


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
                                    var applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(token.Postion, token.Length, SpanTrackingMode.EdgeExclusive);
                                    return new QuickInfoItem(applicableToSpan, token.Decription);
                                }

                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.GetLogger(typeof(QuickInfoSource)).Error("Error with quick info.", ex);
                    }

                    return null;
                });
            }

            public void Dispose()
            {
                _disposed = true;
            }
        }
    }
}
