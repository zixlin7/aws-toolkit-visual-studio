﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Amazon.AWSToolkit.Tests.Common.Context
{
    public class FakeMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
