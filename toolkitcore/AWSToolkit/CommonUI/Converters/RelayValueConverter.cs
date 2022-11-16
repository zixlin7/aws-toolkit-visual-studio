using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    /// <summary>
    /// Value converter implementation that access Convert/ConvertBack delegates.
    /// </summary>
    /// <remarks>
    /// While you can use any delegate assignment technique for Convert/ConvertBack, in XAML, it's easiest to use
    /// {x:StaticResource} with a static field/property on a type.  While that type can be anywhere, you should consider
    /// keeping it close to the consumer, so either the code-behind for a custom control or the view model for a view.
    ///
    /// This class isn't meant to supersede dedicated converters that have high reuse value.  However, this class does
    /// make it easier for custom converters, one-time use converters, and cases where the behavior of a resuable converter
    /// isn't quite what you need, but you don't want to alter the behavior of the existing converter as it is already in
    /// wide-spread use.
    /// </remarks>
    public class RelayValueConverter : DependencyObject, IValueConverter
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(RelayValueConverter));

        /// <summary>
        /// Delegate for the IValueConverter.Convert/ConvertBack methods.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public delegate object ConvertDelegate(object value, Type targetType, object parameter, CultureInfo culture);

        /// <summary>
        /// Identifies the ConvertHandler dependency property.
        /// </summary>
        public static readonly DependencyProperty ConvertHandlerProperty = DependencyProperty.Register(
            nameof(ConvertHandler),
            typeof(ConvertDelegate),
            typeof(RelayValueConverter));

        /// <summary>
        /// Identifies the ConvertBackHandler dependency property.
        /// </summary>
        public static readonly DependencyProperty ConvertBackHandlerProperty = DependencyProperty.Register(
            nameof(ConvertBackHandler),
            typeof(ConvertDelegate),
            typeof(RelayValueConverter));

        /// <summary>
        /// Gets or sets the delegate use to perform IValueConverter.Convert.
        /// </summary>
        public ConvertDelegate ConvertHandler
        {
            get => (ConvertDelegate) GetValue(ConvertHandlerProperty);
            set => SetValue(ConvertHandlerProperty, value);
        }

        /// <summary>
        /// Gets or sets the delegate use to perform IValueConverter.ConvertBack.
        /// </summary>
        public ConvertDelegate ConvertBackHandler
        {
            get => (ConvertDelegate) GetValue(ConvertBackHandlerProperty);
            set => SetValue(ConvertBackHandlerProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the RelayValueConverter class.
        /// </summary>
        public RelayValueConverter()
            : this(null, null) { }

        /// <summary>
        /// Initializes a new instance of the RelayValueConverter class with the specified Convert delegate.
        /// </summary>
        /// <param name="convert">The delegate to use for IValueConverter.Convert</param>
        public RelayValueConverter(ConvertDelegate convert)
            : this(convert, null) { }

        /// <summary>
        /// Initializes a new instance of the RelayValueConverter class with the specific Convert and ConvertBack delegates.
        /// </summary>
        /// <param name="convert">The delegate to use for IValueConverter.Convert</param>
        /// <param name="convertBack">The delegate to use for IValueConverter.ConvertBack</param>
        public RelayValueConverter(ConvertDelegate convert, ConvertDelegate convertBack)
        {
            ConvertHandler = convert;
            ConvertBackHandler = convertBack;
        }

        /// <summary>
        /// Converts a value using the Convert delegate.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        /// <remarks>
        /// If no Convert delegate has been specified, this method returns <paramref name="value"/>.
        /// </remarks>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertHandler?.Invoke(value, targetType, parameter, culture) ?? value;
        }

        /// <summary>
        /// Converts a value using the ConvertBack delegate.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        /// <remarks>
        /// If no ConvertBack delegate has been specified, this method returns <paramref name="value"/>.
        /// </remarks>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertBackHandler?.Invoke(value, targetType, parameter, culture) ?? value;
        }
    }
}
