using System;
using System.Globalization;
using System.Windows.Data;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Credentials.Presentation;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    /// <summary>
    /// "Converts" an AccountViewModel to a grouping based on the credentials type.
    /// </summary>
    public class AccountViewModelGroupConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var account = value as AccountViewModel;
            return account?.Identifier?.GetPresentationGroup() ?? CredentialsIdentifierGroup.AdditionalCredentials;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
