using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    /// <summary>
    /// Used to bind to the ConstantClass object's text
    /// </summary>
    public class ConstantClassConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            var constantClass = value as ConstantClass;
            if (constantClass == null)
            {
                throw new ArgumentException($"Unexpected type: {value.GetType().Name}");
            }

            return constantClass.Value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
