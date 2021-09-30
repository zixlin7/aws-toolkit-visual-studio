using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using Amazon.AWSToolkit.Lambda.Model;

namespace Amazon.AWSToolkit.Lambda.Util
{
    [ValueConversion(sourceType: typeof(LambdaArchitecture), targetType: typeof(bool?))]
    public class ArchitectureToBoolConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter?.Equals(value) ?? false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                return parameter;
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
