using System;
using System.Globalization;
using System.Windows.Data;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    [ValueConversion(typeof(bool?), typeof(bool))]
    public class NullableInverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool?))
            {
                throw new InvalidOperationException("The target must be a nullable boolean");
            }
          
            return !(bool?) value; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
