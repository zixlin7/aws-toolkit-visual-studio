using System.Collections.Generic;

using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Util;

using Xunit;

namespace AWSToolkit.Tests.Publish
{
    public class PublishToAwsExtensionMethodsTests
    {
        public static readonly IEnumerable<object[]> SupportsPublishToAwsExperienceData = new List<object[]>
        {
            new object[] {ToolkitHosts.Vs2017, false},
            new object[] {ToolkitHosts.Vs2019, true},
            new object[] {ToolkitHosts.Vs2022, true},
        };

        [Theory]
        [MemberData(nameof(SupportsPublishToAwsExperienceData))]
        public void SupportsPublishToAwsExperience(IToolkitHostInfo hostInfo, bool expectedResult)
        {
            Assert.Equal(expectedResult, hostInfo.SupportsPublishToAwsExperience());
        }
    }
}
