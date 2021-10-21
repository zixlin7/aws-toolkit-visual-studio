﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Amazon.AWSToolkit.Publish.Converters
{
    /// <summary>
    /// Hides the control when there is no SessionId
    /// </summary>
    public class SessionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Hidden;
            }

            if (value is string str)
            {
                return string.IsNullOrWhiteSpace(str) ? Visibility.Hidden : Visibility.Visible;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
