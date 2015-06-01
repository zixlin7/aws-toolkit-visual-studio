using System;
using System.Windows.Data;
using System.Globalization;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Math converter than can accept a relative operator as part of the parameter, eg
    /// *2 or +10. If no operator is specified, the parameter is added to the supplied value.
    /// </summary>
    /// <remarks>
    /// Only the prefix operators +-*/ are supported; all other operators will return value
    /// unchanged
    /// </remarks>
    public class MathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string param = parameter as string;
            if (param == null || param.Length == 0)
                return value;

            string op = param.Substring(0,1);
            if (char.IsDigit(op, 0))
                return (double)value + double.Parse(param);

            switch (op)
            {
                case "+":
                    return (double)value + double.Parse(param.Substring(1));
                case "-":
                    return (double)value - double.Parse(param.Substring(1));
                case "*":
                    return (double)value * double.Parse(param.Substring(1));
                case "/":
                    return (double)value / double.Parse(param.Substring(1));
                default:
                    return value;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

}
