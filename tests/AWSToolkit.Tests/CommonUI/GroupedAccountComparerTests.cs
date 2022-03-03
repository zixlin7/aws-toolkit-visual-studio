using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.Credentials.Core;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class GroupedAccountComparerTests
    {
        private static readonly ICredentialIdentifier SampleSdkIdA = new SDKCredentialIdentifier("profileA");
        private static readonly ICredentialIdentifier SampleSdkIdB = new SDKCredentialIdentifier("profileB");
        private static readonly ICredentialIdentifier SampleSharedIdA = new SharedCredentialIdentifier("profileA");
        private static readonly ICredentialIdentifier SampleSharedIdB = new SharedCredentialIdentifier("profileB");

        private readonly GroupedAccountComparer _sut = new GroupedAccountComparer();

        [Fact]
        public void DifferentGroups()
        {
            // Sdk credentials are sorted before Shared credentials
            var accountA = CreateAccount(SampleSharedIdA);
            var accountB = CreateAccount(SampleSdkIdB);

            Assert.True(_sut.Compare(accountA, accountB) > 0);
        }

        [Fact]
        public void SameGroups()
        {
            var accountA = CreateAccount(SampleSharedIdA);
            var accountB = CreateAccount(SampleSharedIdB);

            Assert.True(_sut.Compare(accountA, accountB) < 0);
        }

        [Fact]
        public void SameAccount()
        {
            var accountA = CreateAccount(SampleSharedIdA);

            Assert.Equal(0, _sut.Compare(accountA, accountA));
        }

        private AccountViewModel CreateAccount(ICredentialIdentifier credentialIdentifier)
        {
            return new AccountViewModel(null, null, credentialIdentifier, null);
        }
    }
}
