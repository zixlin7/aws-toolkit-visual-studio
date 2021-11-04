using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

using Amazon.AWSToolkit.Publish.Models;

namespace Amazon.AWSToolkit.Publish.Converters
{
    /// <summary>
    /// Determines visibility based on list of <see cref="PublishResource"/>
    /// </summary>
    public class PublishResourcesToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var publishResources = (ObservableCollection<PublishResource>) value;

            if (publishResources?.Any() ?? false)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
