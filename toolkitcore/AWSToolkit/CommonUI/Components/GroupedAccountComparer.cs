using System.Collections;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Credentials.Presentation;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Compares accounts based on a grouping first, then by the account's
    /// Credentials Id.
    /// </summary>
    public class GroupedAccountComparer : IComparer
    {
        private readonly CredentialIdentifierGroupComparer _groupComparer = new CredentialIdentifierGroupComparer();

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

            return _groupComparer.Compare(leftAccount.Identifier, rightAccount.Identifier);
        }
    }
}
