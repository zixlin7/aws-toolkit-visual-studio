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
using Microsoft.VisualStudio.Text.Editor;
using System.Windows.Media;

using Amazon.AWSToolkit.CloudFormation.Parser;
using Amazon.AWSToolkit.CloudFormation.Parser.Schema;
using Amazon.AWSToolkit.CommonUI;
using log4net;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(TemplateContentType.ContentType)]
    [Name("CloudFormationTemplateCompletion")]
    class IntellisenseProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new CompletionSource(textBuffer);
        }


        class CompletionSource : ICompletionSource
        {
            static Dictionary<IntellisenseTokenType, ImageSource> ImageMap;

            static CompletionSource()
            {
                ImageMap = new Dictionary<IntellisenseTokenType, ImageSource>();
                ImageMap[IntellisenseTokenType.ObjectKey] = IconHelper.GetIcon(typeof(CompletionSource).Assembly, "Amazon.AWSToolkit.CloudFormation.EditorExtensions.Resources.Intellisense_ObjectKey.png").Source;
                ImageMap[IntellisenseTokenType.AllowedValue] = IconHelper.GetIcon(typeof(CompletionSource).Assembly, "Amazon.AWSToolkit.CloudFormation.EditorExtensions.Resources.Intellisense_Value.png").Source;
                ImageMap[IntellisenseTokenType.Reference] = IconHelper.GetIcon(typeof(CompletionSource).Assembly, "Amazon.AWSToolkit.CloudFormation.EditorExtensions.Resources.Intellisense_Ref.png").Source;
                ImageMap[IntellisenseTokenType.Condition] = IconHelper.GetIcon(typeof(CompletionSource).Assembly, "Amazon.AWSToolkit.CloudFormation.EditorExtensions.Resources.Intellisense_Ref.png").Source;
                ImageMap[IntellisenseTokenType.NamedArrayElement] = IconHelper.GetIcon(typeof(CompletionSource).Assembly, "Amazon.AWSToolkit.CloudFormation.EditorExtensions.Resources.Intellisense_ObjectKey.png").Source;
                ImageMap[IntellisenseTokenType.IntrinsicFunction] = IconHelper.GetIcon(typeof(CompletionSource).Assembly, "Amazon.AWSToolkit.CloudFormation.EditorExtensions.Resources.Intellisense_IntrinsicFunction.png").Source;
            }


            private ITextBuffer _buffer;
            private bool _disposed = false;

            public CompletionSource(ITextBuffer buffer)
            {
                _buffer = buffer;
            }

            public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
            {
                if (_disposed)
                    throw new ObjectDisposedException("CompletionSource");

                try
                {

                    ITextSnapshot snapshot = _buffer.CurrentSnapshot;
                    var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(snapshot);

                    if (triggerPoint == null)
                        return;

                    var line = triggerPoint.GetContainingLine();
                    SnapshotPoint start = triggerPoint;

                    while (start > line.Start && !char.IsWhiteSpace((start - 1).GetChar()))
                    {
                        start -= 1;
                    }

                    var parser = new TemplateParser();
                    var parserResults = parser.Parse(snapshot.GetText(), triggerPoint.Position);

                    List<Completion> completions = new List<Completion>();
                    foreach (var token in parserResults.IntellisenseTokens.OrderBy(x => x.Type.ToString() + x.DisplayName))
                    {
                        ImageSource source = null;
                        ImageMap.TryGetValue(token.Type, out source);
                        completions.Add(new TemplateCompletion(token.DisplayName, token.Code, token.Description, source, token.Schema));
                    }

                    SnapshotPoint startPoint = triggerPoint;
                    SnapshotPoint endPoint = triggerPoint;

                    if (parserResults.IntellisenseStartingPosition != -1)
                        startPoint = new SnapshotPoint(snapshot, parserResults.IntellisenseStartingPosition);
                    if (parserResults.IntellisenseEndingPosition != -1)
                        endPoint = new SnapshotPoint(snapshot, parserResults.IntellisenseEndingPosition);

                    SnapshotSpan replacementSpan = new SnapshotSpan(startPoint, endPoint);
                    var applicableTo = snapshot.CreateTrackingSpan(replacementSpan, SpanTrackingMode.EdgeInclusive);
                    completionSets.Add(new CompletionSet("All", "All", applicableTo, completions, Enumerable.Empty<Completion>()));
                }
                catch (Exception ex)
                {
                    LogManager.GetLogger(typeof(IntellisenseProvider)).Error("Error in intellisense provider.", ex);
                }
            }

            public void Dispose()
            {
                _disposed = true;
            }
        }
    }

    public class TemplateCompletion : Completion
    {
        public TemplateCompletion(string displayText, string insertionText, string description, ImageSource iconSource, SchemaObject schema)
            : base(displayText, insertionText, description, iconSource, null)
        {
            this.Schema = schema;
        }


        public SchemaObject Schema
        {
            get;
            private set;
        }

    }
}
