using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.Tests.Common.Account
{
    public class AccountFixture
    {
        public static AccountViewModel CreateSharedCredentialAccount()
        {
            return new AccountViewModel(null, null, new SharedCredentialIdentifier("sharedProfile"), null);
        }
    }
}
