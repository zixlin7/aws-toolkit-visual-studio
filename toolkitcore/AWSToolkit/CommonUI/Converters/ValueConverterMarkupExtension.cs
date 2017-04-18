using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    /// <summary>
    /// See http://drwpf.com/blog/2009/03/17/tips-and-tricks-making-value-converters-more-accessible-in-markup/
    /// <typeparam name="T"></typeparam>
    public abstract class ValueConverterMarkupExtension<T> : MarkupExtension, IValueConverter where T : class, IValueConverter, new()
    {
        static T converter;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return converter ?? (converter = new T());
        }

        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

        /// <summary>
        /// Only override this if this converter might be used with 2-way data binding.
        /// </summary>
        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
