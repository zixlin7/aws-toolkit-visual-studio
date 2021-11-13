using System.Collections.Generic;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests
{
    public class IisPathTests
    {
        public static IEnumerable<object[]> CreateIISPathTestCases = new List<object[]>
        {
            new object[] { "", "Default Web Site", "/" },
            new object[] { "Default Web Site", "Default Web Site", "/Default Web Site" },
            new object[] { "Default Web Site/", "Default Web Site", "/" },
            new object[] { "Default Web Site/Application", "Default Web Site", "/Application" },
            new object[] { "Default Web Site/Application    ", "Default Web Site", "/Application" },
            new object[] { "My Site/Application/", "My Site", "/Application" },
            new object[] { "My Site/Application/Dev", "My Site", "/Application/Dev" }
        };

        [Theory]
        [MemberData(nameof(CreateIISPathTestCases))]
        public void ShouldCreateIisPath(string input, string expectedWebSite, string expectedAppPath)
        {
            var iisPath = new IisPath(input);

            Assert.Equal(expectedWebSite, iisPath.WebSite);
            Assert.Equal(expectedAppPath, iisPath.AppPath);
        }
    }
}
