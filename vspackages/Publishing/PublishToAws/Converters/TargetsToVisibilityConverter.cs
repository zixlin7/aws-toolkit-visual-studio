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
    /// Determines visibility based on whether the UI is still loading and if there are any targets <see cref="PublishRecommendation"/>
    /// or <see cref="RepublishTarget"/>
    /// </summary>
    public abstract class TargetsToVisibilityConverter : IMultiValueConverter
    {
     
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Any(x => x == DependencyProperty.UnsetValue) || values.Length < 2)
            {
                return DependencyProperty.UnsetValue;
            }

            var isTargetsLoaded = (bool)values[0];
            var hasTargets = HasTargets(values[1]);

            if (hasTargets || !isTargetsLoaded)
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        protected abstract bool HasTargets(object targets);
    }
}
