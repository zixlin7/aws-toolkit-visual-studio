using System;
using System.Globalization;
using System.Windows.Data;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    ///     Utility class for Xaml to bind against an object's type
    /// </summary>
    public class ObjectToTypeConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value?.GetType();
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}