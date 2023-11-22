using System.Globalization;
using System;
using System.Windows;
using System.Windows.Data;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    public class EqualityVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null
                ? Visibility.Collapsed
                : value is string str && str.Equals(parameter)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
