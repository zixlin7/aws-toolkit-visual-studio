using System.Collections.Generic;

using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.VisualStudio.Utilities.DTE;

using Xunit;

namespace AWSToolkitPackage.Tests.Utilities.DTE
{
    public class DteVersionTests
    {
        public static IEnumerable<object[]> AsHostInfoData =>
            new List<object[]>
            {
                new object[] {string.Empty, ToolkitHosts.VsMinimumSupportedVersion},
                new object[] {null, ToolkitHosts.VsMinimumSupportedVersion},
                new object[] {"1", ToolkitHosts.VsMinimumSupportedVersion},
                new object[] {"999", ToolkitHosts.VsMinimumSupportedVersion},
                new object[] {"X", ToolkitHosts.VsMinimumSupportedVersion},
                new object[] {"12", ToolkitHosts.Vs2013},
                new object[] {"14", ToolkitHosts.Vs2015},
                new object[] {"15", ToolkitHosts.Vs2017},
                new object[] {"15.9", ToolkitHosts.Vs2017},
                new object[] {"15,9", ToolkitHosts.Vs2017},
                new object[] {"16", ToolkitHosts.Vs2019},
                new object[] {"16.9", ToolkitHosts.Vs2019},
                new object[] {"16,9", ToolkitHosts.Vs2019},
            };

        [Theory]
        [MemberData(nameof(AsHostInfoData))]
        public void AsHostInfo(string shellVersion, IToolkitHostInfo expectedResult)
        {
            Assert.Equal(expectedResult, DteVersion.AsHostInfo(shellVersion));
        }
    }
}
