using System;
using System.Globalization;
using System.Windows.Data;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Presentation;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    /// <summary>
    /// "Converts" <see cref="ICredentialIdentifier"/> to a grouping based on the credentials type.
    /// </summary>
    public class CredentialIdentifierGroupConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICredentialIdentifier identifier)
            {
                return identifier.GetPresentationGroup();
            }

            return CredentialsIdentifierGroup.AdditionalCredentials;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
