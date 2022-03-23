using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Presentation;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Presentation
{
    public class CredentialIdentifierGroupComparerTests
    {
        private readonly CredentialIdentifierGroupComparer _sut = new CredentialIdentifierGroupComparer();

        [Fact]
        public void Compare_Id_SameGroup()
        {
            var idA = new SharedCredentialIdentifier("profileA");
            var idB = new SharedCredentialIdentifier("ProfileB");

            Assert.True(_sut.Compare(idA, idB) < 0);
            Assert.True(_sut.Compare(idB, idA) > 0);
            Assert.Equal(0, _sut.Compare(idA, idA));
        }

        [Fact]
        public void Compare_Id_DifferentGroups()
        {
            var shared = new SharedCredentialIdentifier("profileA");
            var sdk = new SDKCredentialIdentifier("profileB");
            var additional = FakeCredentialIdentifier.Create("profileC");

            Assert.True(_sut.Compare(sdk, shared) < 0);
            Assert.True(_sut.Compare(sdk, additional) < 0);
            Assert.True(_sut.Compare(shared, additional) < 0);
            Assert.True(_sut.Compare(additional, shared) > 0);
            Assert.True(_sut.Compare(additional, sdk) > 0);
            Assert.True(_sut.Compare(shared, sdk) > 0);
        }

        [Fact]
        public void Compare_Group_AnotherPriority()
        {
            var groupA = new CredentialsIdentifierGroup() { SortPriority = 1, GroupName = "a", };
            var groupB = new CredentialsIdentifierGroup() { SortPriority = 2, GroupName = "b", };

            Assert.True(_sut.Compare(groupA, groupB) < 0);
            Assert.True(_sut.Compare(groupB, groupA) > 0);
            Assert.Equal(0, _sut.Compare(groupA, groupA));
        }

        [Fact]
        public void Compare_Group_SamePriority()
        {
            var groupA = new CredentialsIdentifierGroup() { SortPriority = 1, GroupName = "a", };
            var groupB = new CredentialsIdentifierGroup() { SortPriority = 1, GroupName = "b", };

            Assert.True(_sut.Compare(groupA, groupB) < 0);
            Assert.True(_sut.Compare(groupB, groupA) > 0);
            Assert.Equal(0, _sut.Compare(groupA, groupA));
        }
    }
}
