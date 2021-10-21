using System.Collections.Generic;
using System.Runtime.Versioning;

using Amazon.AWSToolkit.Tests.Common.SampleData;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Util
{
    public class FrameworkNameHelperTests
    {
        public static IEnumerable<object[]> TargetAndIsFramework = new List<object[]>
        {
            new object[] { SampleFrameworkNames.DotNetFramework472, true },
            new object[] { SampleFrameworkNames.DotNet5, false },
            new object[] { SampleFrameworkNames.Garbage, false },
        };

        public static IEnumerable<object[]> TargetAndIsCore = new List<object[]>
        {
            new object[] { SampleFrameworkNames.DotNetFramework472, false },
            new object[] { SampleFrameworkNames.DotNet5, true },
            new object[] { SampleFrameworkNames.Garbage, false },
        };

        [Theory]
        [MemberData(nameof(TargetAndIsFramework))]
        public void IsDotNetFramework(FrameworkName framework, bool expectedValue)
        {
            Assert.Equal(expectedValue, FrameworkNameHelper.IsDotNetFramework(framework));
        }

        [Theory]
        [MemberData(nameof(TargetAndIsCore))]
        public void IsDotNetCore(FrameworkName framework, bool expectedValue)
        {
            Assert.Equal(expectedValue, FrameworkNameHelper.IsDotNetCore(framework));
        }
    }
}
