using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Amazon.AWSToolkit.CommonUI.Images;

using Microsoft.VisualStudio.PlatformUI;

namespace AwsToolkit.VsSdk.Common.CommonUI.Converters
{
    /// <summary>
    /// Applies appropriate theme to a <see cref="BitmapSource"/> image based on background
    /// </summary>
    public class ImageThemeConverter : IMultiValueConverter
    {
        private readonly IValueConverter _brushToColorConverter;

        public ImageThemeConverter(IValueConverter brushToColorConverter)
        {
            _brushToColorConverter = brushToColorConverter;
        }

        public ImageThemeConverter() : this(new BrushToColorConverter())
        {
        }

        /// <summary>
        /// Required values
        /// Index 0 - The Source property from <see cref="VsImage"/>. This is the image to display.
        /// Index 1 - The <see cref="VsImage"/> being operated on. The Parent chain is examined to find an appropriate background.
        /// Other values are not used, but may be provided to trigger re-calculation.
        ///
        /// In cases where we cannot determine a background, the original image will be used, so that we show *something*.
        /// </summary>
        /// <returns>An <see cref="ImageSource"/> to display, that attempts to account for background colors</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var imageSource = GetValue<BitmapSource>(values, 0);
            if (imageSource == null)
                return DependencyProperty.UnsetValue;

            Brush background = GetParentBackground(GetValue<UserControl>(values, 1));
            if (background == null)
                return imageSource;

            Color? color = (Color?) _brushToColorConverter.Convert(background, typeof(Color), null, null);
            if (color == null)
                return imageSource;

            return MatchImageToBackground(imageSource, color.Value) as BitmapSource;
        }

        private T GetValue<T>(object[] values, int index) where T : class
        {
            if (values == null)
                return null;

            if (values.Length <= index)
                return null;

            return values[index] as T;
        }

        private Brush GetParentBackground(UserControl vsImage)
        {
            if (vsImage == null)
                return null;

            var control = GetParent(vsImage);
            return control?.Background;
        }

        private Control GetParent(FrameworkElement element)
        {
            if (element.Parent is Control parentControl)
            {
                var parent = GetParent(parentControl);
                if (parent == null)
                {
                    return parentControl;
                }

                return parent;
            }

            if (element.Parent is FrameworkElement parentElement)
            {
                return GetParent(parentElement);
            }

            return null;
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
