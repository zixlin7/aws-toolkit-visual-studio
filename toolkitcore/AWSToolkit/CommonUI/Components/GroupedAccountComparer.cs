using System;
using System.Collections;
using System.Windows.Data;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Converters;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Compares accounts based on a grouping first, then by the account's
    /// Credentials Id.
    /// </summary>
    public class GroupedAccountComparer : IComparer
    {
        private readonly IValueConverter _accountToGroupConverter;

        public GroupedAccountComparer()
            : this(new AccountViewModelGroupConverter())
        {
        }

        public GroupedAccountComparer(IValueConverter accountToGroupConverter)
        {
            _accountToGroupConverter = accountToGroupConverter;
        }

        public int Compare(object x, object y)
        {
            var leftAccount = x as AccountViewModel;
            var rightAccount = y as AccountViewModel;

            if (leftAccount == null && rightAccount == null)
            {
                return 0;
            }
            else if (leftAccount == null)
            {
                return 1;
            }
            else if (rightAccount == null)
            {
                return -1;
            }

            // Determine the account's grouping
            var leftGroup =
                _accountToGroupConverter.Convert(leftAccount, typeof(AccountViewModelGroup), null,
                    null) as AccountViewModelGroup;
            var rightGroup =
                _accountToGroupConverter.Convert(rightAccount, typeof(AccountViewModelGroup), null,
                    null) as AccountViewModelGroup;

            // First, compare by group priority
            if (!Equals(leftGroup, rightGroup))
            {
                var leftGroupPriority = leftGroup?.SortPriority ?? int.MinValue;
                var rightGroupPriority = rightGroup?.SortPriority ?? int.MinValue;
                return leftGroupPriority.CompareTo(rightGroupPriority);
            }

            // Then compare by Credentials Id
            return string.Compare(leftAccount.Identifier?.DisplayName, rightAccount.Identifier?.DisplayName,
                StringComparison.InvariantCulture);
        }
    }
}
