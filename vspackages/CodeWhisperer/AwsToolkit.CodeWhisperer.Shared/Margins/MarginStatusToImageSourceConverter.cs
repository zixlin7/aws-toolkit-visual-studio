using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Amazon.AwsToolkit.CodeWhisperer.Resources;
using Amazon.AWSToolkit.CommonUI;

using Microsoft.VisualStudio.PlatformUI;

namespace Amazon.AwsToolkit.CodeWhisperer.Margins
{
    public class MarginStatusToImageSourceConverter : IMultiValueConverter
    {
        private readonly BrushToColorConverter _brushToColorConverter = new BrushToColorConverter();

        /// <summary>
        /// Required values
        /// Index 0 - The Margin status to render in UI - <see cref="MarginStatus"/>
        /// Index 1 - The background brush to render the status image for
        /// </summary>
        /// <remarks>
        /// When either binding value changes, the converter is called. This ensures the image works when
        /// changing between light and dark themes.
        /// </remarks>
        /// <returns>An <see cref="ImageSource"/> to display, that attempts to account for background colors</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var imageSource = GetImage(values);
            var bgColor = GetBackgroundColor(values);

            return imageSource == null || bgColor == null
                ? DependencyProperty.UnsetValue
                : MatchImageToBackground(imageSource, bgColor.Value);
        }

        private Color? GetBackgroundColor(object[] values)
        {
            return values.Length < 2 || !(values[1] is Brush backgroundBrush)
                ? null
                : (Color?) _brushToColorConverter.Convert(backgroundBrush, typeof(Color), null, null);
        }

        private static BitmapSource GetImage(object[] values)
        {
            return values.Length < 1 || !(values[0] is MarginStatus marginStatus)
                ? null
                : GetImage(marginStatus);
        }

        private static BitmapSource GetImage(MarginStatus marginStatus)
        {
            var imageSource = ToolkitImages.CodeWhisperer;

            switch (marginStatus)
            {
                case MarginStatus.Connected:
                    imageSource = ToolkitImages.CodeWhisperer;
                    break;
                case MarginStatus.ConnectedPaused:
                    imageSource = CodeWhispererImages.Paused32;
                    break;
                case MarginStatus.Disconnected:
                    imageSource = CodeWhispererImages.Disconnected32;
                    break;
                case MarginStatus.Error:
                    imageSource = CodeWhispererImages.Error32;
                    break;
            }

            return imageSource as BitmapSource;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Applies correct theming to the bitmap image source based on background color
        /// </summary>
        private ImageSource MatchImageToBackground(BitmapSource source, Color background)
        {
            return ImageThemingUtilities.GetOrCreateThemedBitmapSource(source, background, true, Colors.Black, false);
        }
    }
}
