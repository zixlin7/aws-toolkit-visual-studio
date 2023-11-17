using System;
using System.Collections.Generic;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests
{
    public class VersionRangeTests
    {
        private readonly VersionRange _versionRange = new VersionRange(new Version("1.0.0"), new Version("2.0.0"));

        public static readonly IEnumerable<object[]> VersionData = new[]
        {
            new object[] { new Version("1.1.1"), true },
            new object[] { new Version("1.99.99"), true },
            new object[] { new Version("2.0.0"), false },
            new object[] { new Version("2.1.1"), false },
            new object[] { new Version("0.99.99"), false },
            new object[] { new Version("0.0.0"), false }
        };


        [Theory]
        [MemberData(nameof(VersionData))]
        public void ContainsVersion(Version version, bool expectedResult)
        {
            var result = _versionRange.ContainsVersion(version);
            Assert.Equal(expectedResult, result);
        }
    }
}
