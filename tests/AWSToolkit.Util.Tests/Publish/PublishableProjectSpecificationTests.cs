using System.Collections.Generic;
using System.Runtime.Versioning;

using Amazon.AWSToolkit.Publish;
using Amazon.AWSToolkit.Tests.Common.SampleData;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Publish
{
    public class PublishableProjectSpecificationTests
    {
        public static IEnumerable<object[]> TargetAndIsPublishable = new List<object[]>
        {
            new object[] { SampleFrameworkNames.DotNetFramework472, false },
            new object[] { SampleFrameworkNames.DotNet5, true },
            new object[] { SampleFrameworkNames.Garbage, true },
        };

        [Theory]
        [MemberData(nameof(TargetAndIsPublishable))]
        public void PublishableProjectSpecificationTest(FrameworkName framework, bool expectedValue)
        {
            Assert.Equal(expectedValue, PublishableProjectSpecification.IsSatisfiedBy(framework));
        }
    }
}
