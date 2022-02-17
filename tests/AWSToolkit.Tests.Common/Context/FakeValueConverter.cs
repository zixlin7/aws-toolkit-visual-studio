using System;
using System.Globalization;
using System.Windows.Data;

namespace Amazon.AWSToolkit.Tests.Common.Context
{
    public class FakeValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
