using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonUI.Converters;
using Amazon.AWSToolkit.Credentials.Core;
using AWSToolkit.Tests.Credentials.Core;
using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class AccountViewModelGroupConverterTests
    {
        private readonly AccountViewModelGroupConverter _sut = new AccountViewModelGroupConverter();

        private readonly AccountViewModel _sharedCredentialAccount =
            new AccountViewModel(null, null, new SharedCredentialIdentifier("sharedProfile"), null);

        private readonly AccountViewModel _sdkCredentialAccount =
            new AccountViewModel(null, null, new SDKCredentialIdentifier("sdkProfile"), null);

        private readonly AccountViewModel _alternateCredentialAccount =
            new AccountViewModel(null, null, FakeCredentialIdentifier.Create("altProfile"), null);

        [Fact]
        public void Convert()
        {
            var sharedGroup = Assert.IsType<AccountViewModelGroup>(_sut.Convert(_sharedCredentialAccount,
                typeof(AccountViewModelGroup),
                null, null));

            var sdkGroup = Assert.IsType<AccountViewModelGroup>(_sut.Convert(_sdkCredentialAccount,
                typeof(AccountViewModelGroup),
                null, null));

            var alternateGroup = Assert.IsType<AccountViewModelGroup>(_sut.Convert(_alternateCredentialAccount,
                typeof(AccountViewModelGroup),
                null, null));

            Assert.True(sharedGroup.SortPriority > sdkGroup.SortPriority);
            Assert.True(alternateGroup.SortPriority > sharedGroup.SortPriority);
        }
    }
}
