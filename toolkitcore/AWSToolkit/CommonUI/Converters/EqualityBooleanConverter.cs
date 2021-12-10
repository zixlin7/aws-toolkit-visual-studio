using System;
using System.Globalization;
using System.Windows.Data;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    /// <summary>
    /// Returns true if the given value is equal to the given parameter.
    /// Handy for Enum comparisons.
    /// </summary>
    public class EqualityBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(parameter) ?? parameter == null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool valueBool && valueBool)
            {
                return parameter;
            }

            return Binding.DoNothing;
        }
    }
}
