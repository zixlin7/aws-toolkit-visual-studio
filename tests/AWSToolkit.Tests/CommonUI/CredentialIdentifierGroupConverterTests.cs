using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Converters;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Presentation;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class CredentialIdentifierGroupConverterTests
    {
        private readonly CredentialIdentifierGroupConverter _sut = new CredentialIdentifierGroupConverter();

        public static readonly IEnumerable<object[]> ConvertData = new[]
        {
            new object[] { new SharedCredentialIdentifier("a"), CredentialsIdentifierGroup.SharedCredentials },
            new object[] { new SDKCredentialIdentifier("a"), CredentialsIdentifierGroup.SdkCredentials },
            new object[] { FakeCredentialIdentifier.Create("a"), CredentialsIdentifierGroup.AdditionalCredentials },
            new object[] { "garbage-input", CredentialsIdentifierGroup.AdditionalCredentials },
        };

        [Theory]
        [MemberData(nameof(ConvertData))]
        public void Convert(object input, CredentialsIdentifierGroup expectedResult)
        {
            Assert.Equal(expectedResult, _sut.Convert(input, null, null, null));
        }
    }
}
