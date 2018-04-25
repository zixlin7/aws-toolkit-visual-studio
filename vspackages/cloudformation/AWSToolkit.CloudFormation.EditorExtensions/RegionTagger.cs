using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;

using log4net;

// This code was based on the following MSDN article.
// http://msdn.microsoft.com/en-us/library/ee197665.aspx

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    internal sealed class OutliningTagger : ITagger<IOutliningRegionTag>
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(OutliningTagger));

        public const string COLLAPSED_TEXT = "...";
        ITextBuffer buffer;
        ITextSnapshot snapshot;
        List<Region> regions;

        public OutliningTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
            this.snapshot = buffer.CurrentSnapshot;
            this.regions = new List<Region>();
            this.Reparse();
            this.buffer.Changed += BufferChanged;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            try
            {
                // If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event). 
                if (e.After != buffer.CurrentSnapshot || !determineIfReParseNeeded(e.After, e.Changes))
                    return;
                this.Reparse();
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(typeof(OutliningTagger)).Error("Error with outline tagger.", ex);
            }
        }

        /// <summary>
        /// Only need to reparse if there was a change in line numbers or one of the following char where typed { } [ ]
        /// </summary>
        /// <param name="snapshot"></param>
        /// <param name="changes"></param>
        /// <returns></returns>
        bool determineIfReParseNeeded(ITextSnapshot snapshot, INormalizedTextChangeCollection changes)
        {
            if (changes.IncludesLineChanges)
                return true;

            string document = snapshot.GetText();

            Func<Span, bool> containsRegionChar = span => 
            {
                if (span.IsEmpty)
                    return false;
                var text = document.Substring(span.Start, span.Length);
                if (text.FirstOrDefault(c => c == '{' || c == '}' || c == '[' || c == ']') != default(char))
                    return true;

                return false;
            };

            foreach (var change in changes)
            {
                if (changes.IncludesLineChanges)
                    return true;

                if (change.NewSpan.IsEmpty)
                    continue;

                if (containsRegionChar(change.NewSpan) || containsRegionChar(change.OldSpan))
                    return true;
            }

            return false;
        }

        void Reparse()
        {
            try
            {
                List<Region> newRegions = new List<Region>();

                ITextSnapshot newSnapshot = buffer.CurrentSnapshot;
                Stack<Region> openTokens = new Stack<Region>();
                foreach (var line in newSnapshot.Lines)
                {
                    // foundFirst is a flag indicating the first open character on the line which indicates where we can collapse
                    bool foundFirst = false;
                    string text = line.GetText();
                    for (int i = 0; i < text.Length; i++)
                    {
                        if (text[i] == '[' || text[i] == '{')
                        {
                            var region = new Region()
                            {
                                Visible = !foundFirst,
                                OpenChar = text[i],
                                StartLine = line.LineNumber,
                                StartOffset = i
                            };

                            foundFirst = true;

                            openTokens.Push(region);
                        }
                        else if (text[i] == ']' || text[i] == '}')
                        {
                            if (openTokens.Count == 0)
                                continue;

                            var region = openTokens.Pop();
                            region.EndLine = line.LineNumber;
                            region.EndOffset = i + 1;


                            // Don't create regions for objects that are all on the same line.
                            if (region.StartLine == region.EndLine)
                                region.Visible = false;

                            // Collapse the resource nodes but not the nodes underneath the resource 
                            // so it is easy to expand the resource and see the whole object.
                            if(openTokens.Count == 2)
                                region.IsDefaultCollapsed = true;

                            if(region.Visible)
                                newRegions.Add(region);

                        }
                    }
                }

                //determine the changed span, and send a changed event with the new spans
                List<Span> oldSpans =
                    new List<Span>(this.regions.Select(r => AsSnapshotSpan(r, this.snapshot)
                        .TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive)
                        .Span));
                List<Span> newSpans =
                        new List<Span>(newRegions.Select(r => AsSnapshotSpan(r, newSnapshot).Span));

                NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpans);
                NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpans);

                //the changed regions are regions that appear in one set or the other, but not both.
                NormalizedSpanCollection removed =
                NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

                int changeStart = int.MaxValue;
                int changeEnd = -1;

                if (removed.Count > 0)
                {
                    changeStart = removed[0].Start;
                    changeEnd = removed[removed.Count - 1].End;
                }

                if (newSpans.Count > 0)
                {
                    changeStart = Math.Min(changeStart, newSpans[0].Start);
                    changeEnd = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
                }

                this.snapshot = newSnapshot;
                this.regions = newRegions;

                if (changeStart <= changeEnd)
                {
                    ITextSnapshot snap = this.snapshot;
                    if (this.TagsChanged != null)
                        this.TagsChanged(this, new SnapshotSpanEventArgs(
                            new SnapshotSpan(this.snapshot, Span.FromBounds(changeStart, changeEnd))));
                }
            }
            catch (Exception e)
            {
                LOGGER.Debug("Parse error for regions", e);
            }
        }

        static SnapshotSpan AsSnapshotSpan(Region region, ITextSnapshot snapshot)
        {
            var startLine = snapshot.GetLineFromLineNumber(region.StartLine);
            var endLine = (region.StartLine == region.EndLine) ? startLine
                 : snapshot.GetLineFromLineNumber(region.EndLine);
            return new SnapshotSpan(startLine.Start + region.StartOffset, endLine.End);
        }

        class Region
        {
            public bool Visible { get; set; }
            public char OpenChar { get; set; }
            public int StartLine { get; set; }
            public int StartOffset { get; set; }
            public int EndLine { get; set; }
            public int EndOffset { get; set; }
            public bool IsDefaultCollapsed {get;set;}
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            List<Region> currentRegions = this.regions;
            ITextSnapshot currentSnapshot = this.snapshot;
            SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
            int startLineNumber = entire.Start.GetContainingLine().LineNumber;
            int endLineNumber = entire.End.GetContainingLine().LineNumber;
            foreach (var region in currentRegions)
            {
                if (!region.Visible)
                    continue;

                if (region.StartLine <= endLineNumber &&
                    region.EndLine >= startLineNumber)
                {
                    var startLine = currentSnapshot.GetLineFromLineNumber(region.StartLine);
                    var endLine = currentSnapshot.GetLineFromLineNumber(region.EndLine);

                    var span = new SnapshotSpan(startLine.Start + region.StartOffset, endLine.Start + region.EndOffset);
                    yield return new TagSpan<IOutliningRegionTag>(span,
                        new RegionTag(region.IsDefaultCollapsed, false, currentSnapshot, span));
                }
            }
        }
    }

    internal class RegionTag : IOutliningRegionTag
    {
        const int MAX_TOOLTIP_LINE_NUMBER = 15;
        ITextSnapshot _snapshot;
        SnapshotSpan _span;

        string _displayText;
        string _textTip;

        public RegionTag(bool isDefaultCollapsed, bool isImplementation, ITextSnapshot snapshot, SnapshotSpan span)
        {
            this.IsDefaultCollapsed = isDefaultCollapsed;
            this.IsImplementation = isImplementation;

            this._span = span;
            this._snapshot = snapshot;
        }

        public bool IsDefaultCollapsed { get; private set; }

        public bool IsImplementation { get; private set; }

        public object CollapsedForm
        {
            get
            {
                if (this._displayText == null)
                {
                    this._displayText = ComputeDisplayText();
                }

                return this._displayText;
            }
        }

        public object CollapsedHintForm
        {
            get
            {
                if (this._textTip == null)
                {
                    this._textTip = ComputeToolTip();
                }

                return this._textTip;
            }
        }

        const string DEFAULT_COLLAPSE_TEST = "...";
        const string REGEX_FOR_TYPE = "\"Type\"\\s*:\\s*\"AWS::\\S+::\\S+\"";
        const string REGEX_FOR_DESCRIPTION = "\"Description\"\\s*:\\s*\".+\"";
        string ComputeDisplayText()
        {
            string document = this._snapshot.GetText();
            string section = document.Substring(this._span.Start.Position, this._span.Length);
            MatchCollection matches;
            if ((matches = Regex.Matches(section, REGEX_FOR_TYPE)).Count == 1)
            {
                int startPos = matches[0].Value.IndexOf("AWS::");
                int endPos = matches[0].Value.IndexOf('\"', startPos + 1);
                return "... " + matches[0].Value.Substring(startPos, endPos - startPos) + " ...";
            }
            else if ((matches = Regex.Matches(section, REGEX_FOR_DESCRIPTION)).Count == 1)
            {
                int startPos = matches[0].Value.IndexOf("\"", "\"Description\"".Length + 1);
                if (startPos == -1)
                    return DEFAULT_COLLAPSE_TEST;

                startPos++;
                int endPos = matches[0].Value.IndexOf("\"", startPos);
                if (endPos == -1)
                    return DEFAULT_COLLAPSE_TEST;
                return "... " + matches[0].Value.Substring(startPos, endPos - startPos) + " ...";
            }

            return DEFAULT_COLLAPSE_TEST;
        }

        /// <summary>
        /// Displays the first few lines in this region as a tooltip.
        /// </summary>
        /// <returns></returns>
        string ComputeToolTip()
        {
            StringBuilder sb = new StringBuilder();
            string document = this._snapshot.GetText();
            int i = 0;
            int currentPosition = this._span.Start.Position;
            for (; i < 15; i++)
            {
                bool theEnd = false;
                int endPos = document.IndexOf('\n', currentPosition);
                if (endPos > this._span.Start.Position + this._span.Length || endPos == -1)
                {
                    endPos = this._span.Start.Position + this._span.Length;
                    theEnd = true;
                }

                if (endPos <= currentPosition)
                    sb.AppendLine();
                else
                    sb.AppendLine(document.Substring(currentPosition, endPos - currentPosition).TrimEnd());
                currentPosition = endPos + 1;

                if (theEnd)
                    break;
            }

            if (i == MAX_TOOLTIP_LINE_NUMBER)
            {
                sb.AppendLine("...");
            }

            string value = sb.ToString();
            return value;
        }

    }


    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType(TemplateContentType.ContentType)]
    internal sealed class OutliningTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            //create a single tagger for each buffer.
            Func<ITagger<T>> sc = delegate() { return new OutliningTagger(buffer) as ITagger<T>; };
            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(sc);
        }
    }
}
