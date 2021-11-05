using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Converters
{
    /// <summary>
    /// Determines visibility based on list of <see cref="PublishResource"/>
    /// </summary>
    public class PublishFailureBannerToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var publishStatus = (ProgressStatus) value;

            return (publishStatus == ProgressStatus.Fail) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
