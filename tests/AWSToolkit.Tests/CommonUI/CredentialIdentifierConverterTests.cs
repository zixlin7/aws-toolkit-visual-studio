using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Converters;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class CredentialIdentifierConverterTests
    {
        private readonly CredentialIdentifierConverter _converter = new CredentialIdentifierConverter();
        private readonly ICredentialIdentifier _identifier = FakeCredentialIdentifier.Create("sample-profile");
        private static readonly ToolkitRegion Region =
            new ToolkitRegion { Id = "us-west-2", PartitionId = "aws", DisplayName = "US West (Oregon)" };

        [Fact]
        public void Convert()
        {
            var value = new AwsConnectionSettings(_identifier, null);
            var result = _converter.Convert(value, typeof(string), null, null);
            Assert.Equal(_identifier.DisplayName, result);
        }


        public static IEnumerable<object[]> InvalidInputs = new List<object[]>()
        {
            new object[] { null },
            new object[] { "hello" },
            new object[] { false },
            new object[] { 10 },
            new object[] { new AwsConnectionSettings(null, null)},
            new object[] { new AwsConnectionSettings(null, Region )}
        };

        [Theory]
        [MemberData(nameof(InvalidInputs))]
        public void Convert_WhenInvalidInputs(object value)
        {
            var result = _converter.Convert(value, typeof(string), null, null);

            Assert.Equal(CredentialIdentifierConverter.FallbackValue, result);
        }

        public static IEnumerable<object[]> ParameterData = new List<object[]>()
        {
            new object[] { "hello", "hello" },
            new object[] { false, CredentialIdentifierConverter.FallbackValue },
            new object[] { 10, CredentialIdentifierConverter.FallbackValue }
        };

        [Theory]
        [MemberData(nameof(ParameterData))]
        public void Convert_WhenOverriddenParameter(object parameter, string expectedFallback)
        {
            var result = _converter.Convert(false, typeof(string), parameter, null);
            Assert.Equal(expectedFallback, result);
        }
    }
}
