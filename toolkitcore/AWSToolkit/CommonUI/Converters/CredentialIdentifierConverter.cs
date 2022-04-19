using System;
using System.Globalization;
using System.Windows.Data;

using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    public class CredentialIdentifierConverter : IValueConverter
    {
        public const string FallbackValue = "(not-available)";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AwsConnectionSettings setting)
            {
                return setting.CredentialIdentifier?.DisplayName ?? GetFallBackValue(parameter);
            }

            return GetFallBackValue(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private object GetFallBackValue(object parameter)
        {
            return parameter is string fallBackValue ? fallBackValue : FallbackValue;
        }
    }
}
