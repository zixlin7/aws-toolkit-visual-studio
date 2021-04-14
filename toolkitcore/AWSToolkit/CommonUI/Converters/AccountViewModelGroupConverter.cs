using System;
using System.Globalization;
using System.Windows.Data;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    /// <summary>
    /// "Converts" an AccountViewModel to a grouping based on the credentials type.
    /// </summary>
    public class AccountViewModelGroupConverter : IValueConverter
    {
        private static readonly AccountViewModelGroup SdkCredentialsGroup = new AccountViewModelGroup
        {
            GroupName = ".NET Credentials",
            SortPriority = 1,
        };

        private static readonly AccountViewModelGroup SharedCredentialsGroup = new AccountViewModelGroup
        {
            GroupName = "Shared Credentials", SortPriority = 2,
        };

        private static readonly AccountViewModelGroup AdditionalCredentialsGroup = new AccountViewModelGroup
        {
            GroupName = "Additional Credentials", SortPriority = 3,
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var account = value as AccountViewModel;
            var factoryId = account?.Identifier?.FactoryId;

            switch (factoryId)
            {
                case SharedCredentialProviderFactory.SharedProfileFactoryId:
                    return SharedCredentialsGroup;
                case SDKCredentialProviderFactory.SdkProfileFactoryId:
                    return SdkCredentialsGroup;
                default:
                    // Fall back to a "catch-all" group
                    return AdditionalCredentialsGroup;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
