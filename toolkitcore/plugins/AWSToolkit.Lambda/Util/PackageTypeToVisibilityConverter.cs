using Amazon.Lambda;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Amazon.AWSToolkit.Lambda.Util
{
    /// <summary>
    /// Allows Visibility DataBinding against a PackageType value
    /// </summary>
    public class PackageTypeToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts the specified PackageType to Visible when it is the expected PackageType,
        /// otherwise Collapsed.
        /// </summary>
        /// <param name="value">PackageType value to convert to a Visibility value</param>
        /// <param name="parameter">The PackageType value to make Visible</param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var expectedPackageType = parameter as PackageType;
            if (expectedPackageType == null) return DependencyProperty.UnsetValue;

            var packageType = value as PackageType;
            if (packageType == null) return DependencyProperty.UnsetValue;

            return packageType == expectedPackageType ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}