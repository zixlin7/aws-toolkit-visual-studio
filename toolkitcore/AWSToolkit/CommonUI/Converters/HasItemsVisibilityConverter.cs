using System;
using System.Collections;
using System.Globalization;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    public sealed class HasItemsVisibilityConverter : ValueConverterMarkupExtension<HasItemsVisibilityConverter>
    {
        readonly BooleanToVisibilityConverter _converter = new BooleanToVisibilityConverter();

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IList ic = value as IList;
            return _converter.Convert(ic != null && ic.Count > 0, targetType, parameter, culture);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
