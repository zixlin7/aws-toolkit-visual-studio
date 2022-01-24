using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    /// <summary>
    /// Chains Converters together
    /// Inspiration: https://dzone.com/articles/xaml-and-converters-chaining
    /// </summary>
    [ContentProperty(nameof(Converters))]
    public class ConverterPipeline : IValueConverter
    {
        public Collection<IValueConverter> Converters { get; set; } = new Collection<IValueConverter>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var converter in Converters)
            {
                value = converter.Convert(value, targetType, parameter, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var reversedConverters = Converters.ToList();
            reversedConverters.Reverse();

            foreach (var converter in reversedConverters)
            {
                value = converter.ConvertBack(value, targetType, parameter, culture);
            }

            return value;
        }
    }
}
