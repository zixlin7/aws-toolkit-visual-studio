using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Tests.Common.Context;

namespace Amazon.AWSToolkit.Tests.Common.Account
{
    public class AccountFixture
    {
        public static AccountViewModel CreateSharedCredentialAccount()
        {
            return CreateAccountWith(new SharedCredentialIdentifier("sharedProfile"));
        }

        private static AccountViewModel CreateAccountWith(ICredentialIdentifier credentialIdentifier)
        {
            return new AccountViewModel(null, null, credentialIdentifier, null);
        }

        public static AccountViewModel CreateSdkCredentialAccount()
        {
            return CreateAccountWith(new SDKCredentialIdentifier("sdkProfile"));
        }

        public static AccountViewModel CreateFakeCredentialAccount()
        {
            return CreateAccountWith(FakeCredentialIdentifier.Create("altProfile"));
        }
    }
}
