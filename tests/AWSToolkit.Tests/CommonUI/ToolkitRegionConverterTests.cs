using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Converters;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class ToolkitRegionConverterTests
    {
        private readonly ToolkitRegionConverter _converter = new ToolkitRegionConverter();
        private readonly ToolkitRegion _region =
            new ToolkitRegion { Id = "sample-id", PartitionId = "sample-partition", DisplayName = "sample (Region)" };
        private static readonly ICredentialIdentifier Identifier = FakeCredentialIdentifier.Create("sample-profile");

        [Fact]
        public void Convert()
        {
            var value = new AwsConnectionSettings(null, _region);
            var result = _converter.Convert(value, typeof(string), null, null);
            Assert.Equal(_region.DisplayName, result);
        }


        public static IEnumerable<object[]> InvalidInputs = new List<object[]>()
        {
            new object[] { null },
            new object[] { "hello" },
            new object[] { false },
            new object[] { 10 },
            new object[] { new AwsConnectionSettings(null, null) },
            new object[] { new AwsConnectionSettings(Identifier, null) }
        };

        [Theory]
        [MemberData(nameof(InvalidInputs))]
        public void Convert_WhenInvalidInputs(object value)
        {
            var result = _converter.Convert(value, typeof(string), null, null);

            Assert.Equal(ToolkitRegionConverter.FallbackValue, result);
        }

        public static IEnumerable<object[]> ParameterData = new List<object[]>()
        {
            new object[] { "hello", "hello" },
            new object[] { false, ToolkitRegionConverter.FallbackValue },
            new object[] { 10, ToolkitRegionConverter.FallbackValue }
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
