using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

using Amazon.AWSToolkit.Publish.Models;

namespace Amazon.AWSToolkit.Publish.Converters
{
    /// <summary>
    /// Controls which ConfigurationDetail children are rendered
    /// </summary>
    public class ConfigurationDetailChildrenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IamRoleConfigurationDetail)
            {
                return Enumerable.Empty<ConfigurationDetail>();
            }

            if (value is ConfigurationDetail detail)
            {
                return detail.Children;
            }

            return Enumerable.Empty<ConfigurationDetail>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
