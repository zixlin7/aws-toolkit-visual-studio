using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonUI.Converters;
using Amazon.AWSToolkit.Credentials.Presentation;
using Amazon.AWSToolkit.Tests.Common.Account;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class AccountViewModelGroupConverterTests
    {
        private readonly AccountViewModelGroupConverter _sut = new AccountViewModelGroupConverter();

        private readonly AccountViewModel _sharedCredentialAccount = AccountFixture.CreateSharedCredentialAccount();
        private readonly AccountViewModel _sdkCredentialAccount = AccountFixture.CreateSdkCredentialAccount();
        private readonly AccountViewModel _alternateCredentialAccount = AccountFixture.CreateFakeCredentialAccount();

        [Fact]
        public void Convert()
        {
            var sharedGroup = Assert.IsType<CredentialsIdentifierGroup>(_sut.Convert(_sharedCredentialAccount,
                typeof(CredentialsIdentifierGroup),
                null, null));

            var sdkGroup = Assert.IsType<CredentialsIdentifierGroup>(_sut.Convert(_sdkCredentialAccount,
                typeof(CredentialsIdentifierGroup),
                null, null));

            var alternateGroup = Assert.IsType<CredentialsIdentifierGroup>(_sut.Convert(_alternateCredentialAccount,
                typeof(CredentialsIdentifierGroup),
                null, null));

            Assert.True(sharedGroup.SortPriority > sdkGroup.SortPriority);
            Assert.True(alternateGroup.SortPriority > sharedGroup.SortPriority);
        }
    }
}
