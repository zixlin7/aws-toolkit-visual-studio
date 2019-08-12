using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Amazon.AWSToolkit.CloudFormation.Parser;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(TemplateContentType.ContentType)]
    class TemplateSyntaxHighlighterProvider : IClassifierProvider
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TemplateSyntaxHighlighterProvider));

        [Import]
        IClassificationTypeRegistryService ClassificationRegistry = null;

        [Import]
        internal ITextSearchService TextSearchService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            var classifier = buffer.Properties.GetOrCreateSingletonProperty(() => new TemplateSyntaxHighlighter(buffer, ClassificationRegistry, TextSearchService));
            LOGGER.Debug("Creating CloudFormation SyntaxHighlighter");
            return classifier;
        }

        class TemplateSyntaxHighlighter : IClassifier
        {
            ILog LOGGER = LogManager.GetLogger(typeof(TemplateSyntaxHighlighter));

            static Dictionary<VsTheme, Dictionary<TemplateTokenType, string>> ThemeTokenMap;

            static TemplateSyntaxHighlighter()
            {
                ThemeTokenMap = new Dictionary<VsTheme, Dictionary<TemplateTokenType, string>>();
                ThemeTokenMap[VsTheme.Light] = SetupThemeClassiferMap(CloudFormationKeysFormat.NAME, GenericLiteralFormat.NAME, CloudFormationIntrinsicFunctionFormat.NAME, ErrorFormat.NAME);
                ThemeTokenMap[VsTheme.Blue] = ThemeTokenMap[VsTheme.Light];
                ThemeTokenMap[VsTheme.Unknown] = ThemeTokenMap[VsTheme.Light];

                ThemeTokenMap[VsTheme.Dark] = SetupThemeClassiferMap(CloudFormationKeysDarkThemeFormat.NAME, GenericLiteralDarkThemeFormat.NAME, CloudFormationIntrinsicFunctionDarkThemeFormat.NAME, ErrorDarkThemeFormat.NAME);
            }

            static Dictionary<TemplateTokenType, string> SetupThemeClassiferMap(string validKey, string genericLiteral, string intrinsicFunctionName, string error)
            {
                var map = new Dictionary<TemplateTokenType, string>();

                map[TemplateTokenType.ValidKey] = validKey;
                map[TemplateTokenType.ScalerValue] = genericLiteral;

                map[TemplateTokenType.IntrinsicFunction] = intrinsicFunctionName;
                map[TemplateTokenType.InvalidTypeReference] = error;
                map[TemplateTokenType.UnknownReference] = error;
                map[TemplateTokenType.UnknownResource] = error;
                map[TemplateTokenType.UnknownResourceAttribute] = error;

                map[TemplateTokenType.InvalidKey] = error;
                map[TemplateTokenType.DuplicateKey] = error;
                map[TemplateTokenType.NotAllowedValue] = error;

                map[TemplateTokenType.UnknownMapName] = error;
                map[TemplateTokenType.UnknownMapKey] = error;
                map[TemplateTokenType.UnknownMapValue] = error;

                map[TemplateTokenType.UnknownConditionType] = error;
                map[TemplateTokenType.UnknownConditionTrue] = error;
                map[TemplateTokenType.UnknownConditionFalse] = error;

                return map;
            }

            public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

            IClassificationTypeRegistryService _classificationRegistry;
            ITextBuffer _buffer;
            ITextSearchService TextSearchService { get; }


            object updateLock = new object();
            List<ClassificationSpan> CurrentSpans { get; set; }
            ITextSnapshot CurrentSnapshot { get; set; }
            ITextSnapshot RequestSnapshot { get; set; }

            Guid _lastKickOff;

            public TemplateSyntaxHighlighter(ITextBuffer buffer, IClassificationTypeRegistryService classificationRegistry, ITextSearchService textSearchService)
            {
                _classificationRegistry = classificationRegistry;
                this.TextSearchService = textSearchService;
                _buffer = buffer;
                _buffer.Changed += BufferChanged;

                // Do initial scan of spans
                var args = new TextContentChangedEventArgs(_buffer.CurrentSnapshot, _buffer.CurrentSnapshot, EditOptions.None, null);
                BufferChanged(this, args);
            }

            public void TriggerBufferChanged()
            {
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
                    Thread.Sleep(300);
                    if ((Guid)threadContext != this._lastKickOff)
                        return;

                    List<SnapshotSpan> wordSpans = new List<SnapshotSpan>();

                    var currentTextSnapshot = RequestSnapshot;

                    var parser = new TemplateParser();
                    var parserResults = parser.Parse(currentTextSnapshot.GetText());

                    // Store parse results for other components to use instead of reparsing the document.
                    this._buffer.Properties[EditorContants.LAST_TEXT_BUFFER_PARSE_RESULTS] = parserResults;

                    Dictionary<TemplateTokenType, string> templateTokenToClassiferMap;
                    if (!ThemeTokenMap.TryGetValue(ThemeUtil.GetCurrentTheme(), out templateTokenToClassiferMap))
                        templateTokenToClassiferMap = ThemeTokenMap[VsTheme.Unknown];

                    List<ClassificationSpan> spans = new List<ClassificationSpan>();
                    foreach (var templateToken in parserResults.HighlightedTemplateTokens)
                    {
                        if (currentTextSnapshot.Length <= templateToken.Postion)
                            continue;

                        string classificationType = null;
                        if (!templateTokenToClassiferMap.TryGetValue(templateToken.Type, out classificationType))
                            continue;

                        var classType = _classificationRegistry.GetClassificationType(classificationType);

                        int length = templateToken.Length;
                        if (currentTextSnapshot.Length <= templateToken.Postion + length)
                            length = currentTextSnapshot.Length - templateToken.Postion;

                        var snapshotSpan = new SnapshotSpan(currentTextSnapshot, new Span(templateToken.Postion, length));
                        var span = new ClassificationSpan(snapshotSpan, classType);
                        spans.Add(span);
                    }

                    // If we are still up-to-date (another change hasn't happened yet), do a real update
                    if (currentTextSnapshot == RequestSnapshot)
                        SynchronousUpdate(currentTextSnapshot, spans);
                }
                catch (Exception e)
                {
                    LOGGER.Error("Error processing document for color syntax", e);
                }
            }

            /// <summary>
            /// Perform a synchronous update, in case multiple background threads are running
            /// </summary>
            void SynchronousUpdate(ITextSnapshot currentTextSnapshot, List<ClassificationSpan> requestSpans)
            {
                lock (updateLock)
                {
                    if (currentTextSnapshot != RequestSnapshot)
                        return;

                    CurrentSpans = requestSpans;
                    CurrentSnapshot = currentTextSnapshot;

                    var tempEvent = ClassificationChanged;
                    if (tempEvent != null)
                        tempEvent(this, new ClassificationChangedEventArgs(new SnapshotSpan(CurrentSnapshot, 0, CurrentSnapshot.Length)));
                }
            }

            public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
            {
                try
                {
                    if (CurrentSnapshot == null)
                        return new List<ClassificationSpan>();

                    // Hold on to a "snapshot" of the word spans and current word, so that we maintain the same
                    // collection throughout
                    IList<ClassificationSpan> currentSpans = new List<ClassificationSpan>();
                    var tempAssignment = this.CurrentSpans; // Assign just in case another thread changes CurrentSpans
                    foreach (var s in tempAssignment)
                    {
                        if (s.Span.Start.Position <= span.End.Position && span.Start.Position <= s.Span.End.Position)
                            currentSpans.Add(s);
                    }

                    if (currentSpans.Count == 0)
                        return new List<ClassificationSpan>();

                    // If the requested snapshot isn't the same as the one our words are on, translate our spans
                    // to the expected snapshot
                    if (span.Snapshot != currentSpans[0].Span.Snapshot)
                    {
                        var newSpans = new List<ClassificationSpan>();
                        foreach (var s in currentSpans)
                        {
                            var ts = new ClassificationSpan(s.Span.TranslateTo(span.Snapshot, SpanTrackingMode.EdgeExclusive), s.ClassificationType);
                            newSpans.Add(ts);
                        }

                        currentSpans = newSpans;
                    }

                    return currentSpans;
                }
                catch (Exception e)
                {
                    LOGGER.Error("Error Getting classification spans for color syntax", e);
                    return new List<ClassificationSpan>();
                }
            }
        }
    }

    #region Light Theme
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = CloudFormationKeysFormat.NAME)]
    [Name("CloudFormation Template Keywords")]
    [UserVisible(true)]
    internal sealed class CloudFormationKeysFormat : ClassificationFormatDefinition
    {
        internal const string NAME = "template.cloudformationkey";

        [Export]
        [Name(NAME)]
        internal static ClassificationTypeDefinition CloudFormationKeysDefinition = null;

        public CloudFormationKeysFormat()
        {
            this.ForegroundColor = Colors.Green;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = CloudFormationIntrinsicFunctionFormat.NAME)]
    [Name("CloudFormation Template Intrinsic Function")]
    [UserVisible(true)]
    internal sealed class CloudFormationIntrinsicFunctionFormat : ClassificationFormatDefinition
    {
        internal const string NAME = "template.cloudformationintrinsicfunction";

        [Export]
        [Name(NAME)]
        internal static ClassificationTypeDefinition CloudFormationIntrinsicFunctionDefinition = null;

        public CloudFormationIntrinsicFunctionFormat()
        {
            this.ForegroundColor = Colors.Purple;
        }


    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = GenericLiteralFormat.NAME)]
    [Name("CloudFormation Template Literals")]
    [UserVisible(true)]
    internal sealed class GenericLiteralFormat : ClassificationFormatDefinition
    {
        internal const string NAME = "template.genericliteral";

        [Export]
        [Name(NAME)]
        internal static ClassificationTypeDefinition GenericStringLiteralDefinition = null;

        public GenericLiteralFormat()
        {
            this.ForegroundColor = Colors.Blue;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = ErrorFormat.NAME)]
    [Name("CloudFormation Template Error")]
    [UserVisible(true)]
    internal sealed class ErrorFormat : ClassificationFormatDefinition
    {
        internal const string NAME = "template.error";

        [Export]
        [Name(NAME)]
        internal static ClassificationTypeDefinition ErrorDefinition = null;

        public ErrorFormat()
        {
            this.ForegroundColor = Colors.Red;
        }
    }
    #endregion


    #region Dark Themed

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = CloudFormationKeysDarkThemeFormat.NAME)]
    [Name("CloudFormation Template Keywords (Dark Theme)")]
    [UserVisible(true)]
    internal sealed class CloudFormationKeysDarkThemeFormat : ClassificationFormatDefinition
    {
        internal const string NAME = "template.cloudformationkey.darktheme";

        [Export]
        [Name(NAME)]
        internal static ClassificationTypeDefinition CloudFormationKeysDefinition = null;

        public CloudFormationKeysDarkThemeFormat()
        {
            this.ForegroundColor = Colors.Green;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = CloudFormationIntrinsicFunctionDarkThemeFormat.NAME)]
    [Name("CloudFormation Template Intrinsic Function (Dark Theme)")]
    [UserVisible(true)]
    internal sealed class CloudFormationIntrinsicFunctionDarkThemeFormat : ClassificationFormatDefinition
    {
        internal const string NAME = "template.cloudformationintrinsicfunction.darktheme";

        [Export]
        [Name(NAME)]
        internal static ClassificationTypeDefinition CloudFormationIntrinsicFunctionDefinition = null;

        public CloudFormationIntrinsicFunctionDarkThemeFormat()
        {
            this.ForegroundColor = Colors.Orange;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = GenericLiteralDarkThemeFormat.NAME)]
    [Name("CloudFormation Template Literals (Dark Theme)")]
    [UserVisible(true)]
    internal sealed class GenericLiteralDarkThemeFormat : ClassificationFormatDefinition
    {
        internal const string NAME = "template.genericliteral.darktheme";

        [Export]
        [Name(NAME)]
        internal static ClassificationTypeDefinition GenericStringLiteralDefinition = null;

        public GenericLiteralDarkThemeFormat()
        {
            this.ForegroundColor = new Color() { R = 91, G = 153, B = 255 };
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = ErrorDarkThemeFormat.NAME)]
    [Name("CloudFormation Template Error (Dark Theme)")]
    [UserVisible(true)]
    internal sealed class ErrorDarkThemeFormat : ClassificationFormatDefinition
    {
        internal const string NAME = "template.error.darktheme";

        [Export]
        [Name(NAME)]
        internal static ClassificationTypeDefinition ErrorDefinition = null;

        public ErrorDarkThemeFormat()
        {
            this.ForegroundColor = Colors.Red;
        }
    }
    #endregion
}