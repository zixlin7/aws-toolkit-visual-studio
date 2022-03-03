using System.Collections.Generic;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Presentation;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Presentation
{
    public class CredentialIdentifierExtensionMethodsTests
    {
        public static readonly IEnumerable<object[]> Identifiers = new[]
        {
            new object[] { new SharedCredentialIdentifier("foo"), CredentialsIdentifierGroup.SharedCredentials },
            new object[] { new SDKCredentialIdentifier("foo"), CredentialsIdentifierGroup.SdkCredentials },
            new object[] { FakeCredentialIdentifier.Create("foo"), CredentialsIdentifierGroup.AdditionalCredentials },
            new object[] { null, CredentialsIdentifierGroup.AdditionalCredentials },
        };

        [Theory]
        [MemberData(nameof(Identifiers))]
        public void GetPresentationGroup(ICredentialIdentifier credentialIdentifier,
            CredentialsIdentifierGroup expectedGroup)
        {
            Assert.Equal(expectedGroup, credentialIdentifier.GetPresentationGroup());
        }
    }
}
