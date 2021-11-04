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
    /// Determines visibility of message box indicating there are no editable settings based on list of <see cref="ConfigurationDetails"/>
    /// filtered by the view i.e Core settings or Advanced settings
    /// </summary>
    public class ConfigurationDetailsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var configDetails = (ObservableCollection<ConfigurationDetail>) value;
            if (NoConfigurationDetails(configDetails))
            {
                return Visibility.Visible;
            }

            if (!bool.TryParse(parameter?.ToString(), out var isCore))
            {
                throw new ArgumentException("The parameter must be a boolean");
            }

            var visibleDetails = configDetails.ToList().Where(detail => IsVisibleForDetail(detail, isCore)).ToList();

            return visibleDetails.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        private bool IsVisibleForDetail(ConfigurationDetail detail, bool isCore)
        {
            return isCore ? IsVisibleForCore(detail) : IsVisibleForAdvanced(detail);
        }

        private bool IsVisibleForAdvanced(ConfigurationDetail detail)
        {
            return detail.Visible;
        }

        private bool IsVisibleForCore(ConfigurationDetail detail)
        {
            return !detail.Advanced && detail.Visible;
        }

        private bool NoConfigurationDetails(ObservableCollection<ConfigurationDetail> configDetails)
        {
            return !configDetails?.Any() ?? true;
        }
    }
}
