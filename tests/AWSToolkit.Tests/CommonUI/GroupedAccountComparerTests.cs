using System;
using System.Windows.Data;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.Credentials.Core;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class GroupedAccountComparerTests
    {
        private readonly Mock<IValueConverter> _comparer = new Mock<IValueConverter>();
        private readonly GroupedAccountComparer _sut;

        private readonly AccountViewModel _accountA =
            new AccountViewModel(null, null, new SharedCredentialIdentifier("profileA"), null);

        private readonly AccountViewModel _accountB =
            new AccountViewModel(null, null, new SharedCredentialIdentifier("profileB"), null);

        private readonly AccountViewModelGroup _group1 =
            new AccountViewModelGroup() {GroupName = "group 1", SortPriority = 1};

        private readonly AccountViewModelGroup _group2 =
            new AccountViewModelGroup() {GroupName = "group 2", SortPriority = 2};

        public GroupedAccountComparerTests()
        {
            _sut = new GroupedAccountComparer(_comparer.Object);
        }

        [Fact]
        public void DifferentGroups()
        {
            // Account B is in a group that is sorted before Account A
            DeclareAccountGroup(_accountA, _group2);
            DeclareAccountGroup(_accountB, _group1);

            Assert.True(_sut.Compare(_accountA, _accountB) > 0);
        }

        [Fact]
        public void SameGroups()
        {
            DeclareAccountGroup(_accountA, _group1);
            DeclareAccountGroup(_accountB, _group1);

            Assert.True(_sut.Compare(_accountA, _accountB) < 0);
        }

        [Fact]
        public void SameAccount()
        {
            DeclareAccountGroup(_accountA, _group1);

            Assert.Equal(0, _sut.Compare(_accountA, _accountA));
        }

        private void DeclareAccountGroup(AccountViewModel account, AccountViewModelGroup group)
        {
            _comparer.Setup(mock => mock.Convert(account, It.IsAny<Type>(), null, null))
                .Returns(() => group);
        }
    }
}
