using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using log4net;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("CloudFormation Template QuickInfo Listener")]
    [ContentType(TemplateContentType.ContentType)]
    internal class QuickInfoControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        internal IAsyncQuickInfoBroker QuickInfoBroker { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new QuickInfoListener(textView, subjectBuffers, this);
        }
    }

    internal class QuickInfoListener : IIntellisenseController
    {
        private ITextView _textView;
        private IList<ITextBuffer> _subjectBuffers;
        private QuickInfoControllerProvider _componentContext;

        internal QuickInfoListener(ITextView textView, IList<ITextBuffer> subjectBuffers, QuickInfoControllerProvider componentContext)
        {
            _textView = textView;
            _subjectBuffers = subjectBuffers;
            _componentContext = componentContext;

            _textView.MouseHover += this.OnTextViewMouseHover;
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
                _textView.MouseHover -= this.OnTextViewMouseHover;
                _textView = null;
            }
        }

        #endregion

        private void OnTextViewMouseHover(object sender, MouseHoverEventArgs e)
        {
            try
            {
                SnapshotPoint? point = this.GetMousePosition(new SnapshotPoint(_textView.TextSnapshot, e.Position));

                if (point != null)
                {
                    ITrackingPoint triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position,
                        PointTrackingMode.Positive);

                    // Find the broker for this buffer

                    if (!_componentContext.QuickInfoBroker.IsQuickInfoActive(_textView))
                    {
                        this._componentContext.QuickInfoBroker.TriggerQuickInfoAsync(_textView, triggerPoint, QuickInfoSessionOptions.TrackMouse);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(typeof(QuickInfoListener)).Error("Error with quick info.", ex);
            }
        }

        private SnapshotPoint? GetMousePosition(SnapshotPoint topPosition)
        {
            // Map this point down to the appropriate subject buffer.
            return _textView.BufferGraph.MapDownToFirstMatch
                (
                topPosition,
                PointTrackingMode.Positive,
                snapshot => _subjectBuffers.Contains(snapshot.TextBuffer),
                PositionAffinity.Predecessor
                );
        }
    }
}
