using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.VisualStudio.PlatformUI;

namespace AwsToolkit.VsSdk.Common.CommonUI.Converters
{
    /// <summary>
    /// Applies appropriate theme to a <see cref="BitmapSource"/> image based on background
    /// </summary>
    public class ImageThemeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2 || !(values[0] is BitmapSource originalImage) || !(values[1] is Color backgroundColor))
            {
                return DependencyProperty.UnsetValue;
            }

            return ThemeImage(backgroundColor, originalImage) as BitmapSource;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Applies correct theming to the bitmap image source based on background color
        /// </summary>
        private ImageSource ThemeImage(Color background, BitmapSource source)
        {
            return ImageThemingUtilities.GetOrCreateThemedBitmapSource(source, background, true, Colors.Black, false);
        }
    }
}
