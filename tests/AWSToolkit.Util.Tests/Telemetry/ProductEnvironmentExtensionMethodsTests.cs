using System;

using Amazon.AWSToolkit.Telemetry.Model;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    public class ProductEnvironmentExtensionMethodsTests
    {
        public static TheoryData<string, bool, Version> GetBaseParentInputs()
        {
            return new TheoryData<string, bool, Version>()
            {
                { "17.7", true, new Version(17, 7) },
                { "17.7.6", true, new Version(17, 7, 6) },
                { "17.8.0 Preview", true, new Version(17, 8, 0) },
                { "unknown", false, null },
                { "", false, null },
                { "foo", false, null },
            };
        }

        [Theory]
        [MemberData(nameof(GetBaseParentInputs))]
        public void TryGetBaseParentProductVersion(string inputText, bool expectedResult, Version expectedVersion)
        {
            var sut = new ProductEnvironment() { ParentProductVersion = inputText, };

            Assert.Equal(expectedResult, sut.TryGetBaseParentProductVersion(out var baseVersion));
            Assert.Equal(expectedVersion, baseVersion);
        }
    }
}
