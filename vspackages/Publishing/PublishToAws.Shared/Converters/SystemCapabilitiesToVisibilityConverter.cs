using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

using Amazon.AWSToolkit.Publish.Models;

namespace Amazon.AWSToolkit.Publish.Converters
{
    public class SystemCapabilitiesToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var systemCapabilities = (ObservableCollection<TargetSystemCapability>) value;
            if (systemCapabilities?.Any() ?? false)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
