using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Draws a shade over a given UIElement with a customizable 'Loading...'
    /// message.
    /// </summary>
    public class LoadingMessageAdorner : Adorner
    {
        Brush _backgroundBrush;
        Pen _borderPen;
        Brush _messageBrush;
        Typeface _typeface;
        double _messagePointSize;

        public LoadingMessageAdorner(UIElement adornerElement)
            : base(adornerElement)
        {
            _backgroundBrush = new SolidColorBrush(Color.FromRgb(192, 192, 192));
            _backgroundBrush.Opacity = 0.25;
            _borderPen = new Pen(_backgroundBrush, 1);
            _typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Italic, FontWeights.Bold, FontStretches.Normal);
            _messagePointSize = 24;
            // found a darker brush, but not full black, works better :-)
            _messageBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
        }

        public LoadingMessageAdorner(UIElement adornerElement, string message)
            : this(adornerElement)
        {
            Message = message;
        }

        public string Message { get; set; }

        public Typeface MessageTypeface
        {
            get { return _typeface; }
            set { _typeface = value; }
        }

        public double MessagePointSize
        {
            get { return _messagePointSize; }
            set { _messagePointSize = value; }
        }

        public Brush MessageBrush
        {
            get { return _messageBrush; }
            set { _messageBrush = value; }
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Size overlaySize = AdornedElement.RenderSize;

            drawingContext.DrawRoundedRectangle(_backgroundBrush, _borderPen, new Rect(new Point(0, 0), new Point(overlaySize.Width, overlaySize.Height)), 4, 4);

            if (!string.IsNullOrEmpty(Message))
            {
                FormattedText ft = new FormattedText(Message,
                                                     System.Globalization.CultureInfo.CurrentCulture,
                                                     FlowDirection.LeftToRight,
                                                     _typeface,
                                                     _messagePointSize,
                                                     _messageBrush);

                ft.MaxTextWidth = overlaySize.Width - 20;
                ft.MaxTextHeight = overlaySize.Height - 20;
                ft.Trimming = TextTrimming.WordEllipsis;
                ft.TextAlignment = TextAlignment.Center;

                // rendering about a 1/3 of the way down seems more 'attractive' to me
                drawingContext.DrawText(ft, new Point((overlaySize.Width - ft.Width) / 2, (overlaySize.Height - ft.Height) / 3));
            }
        }
    }
}
