using System;
using Amazon.AWSToolkit.ElasticBeanstalk.Utils;
using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class SolutionStackUtilsTests
    {
        private readonly Version _defaultVersion = new Version("1.0.0");

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("64bit Amazon Linux 2018.03 v4.14.4 running Node.js", false)]
        [InlineData("64bit Windows Server 2019 v2.5.6 running IIS 10.0", true)]
        public void SolutionStackIsWindows(string stackName, bool expectedResult)
        {
            Assert.Equal(expectedResult, SolutionStackUtils.SolutionStackIsWindows(stackName));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("64bit Amazon Linux 2018.03 v4.14.4 running Node.js", false)]
        [InlineData("64bit legacy Windows Server 2019 v2.5.6 running IIS 10.0", true)]
        public void SolutionStackIsLegacy(string stackName, bool expectedResult)
        {
            Assert.Equal(expectedResult, SolutionStackUtils.SolutionStackIsLegacy(stackName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public void ParseVersionFromSolutionStack_WithNoText(string stackName)
        {
            var version = SolutionStackUtils.ParseVersionFromSolutionStack(stackName, _defaultVersion);
            Assert.StrictEqual(_defaultVersion, version);
        }

        [Theory]
        [InlineData("63bit versionless running everything")]
        [InlineData("64bit Windows Server Core 2019 v.5 running IIS 10.0")]
        public void ParseVersionFromSolutionStack_WithBadVersion(string stackName)
        {
            var version = SolutionStackUtils.ParseVersionFromSolutionStack(stackName, _defaultVersion);
            Assert.StrictEqual(_defaultVersion, version);
        }

        [Fact]
        public void ParseVersionFromSolutionStack_WithVersion()
        {
            var version = SolutionStackUtils.ParseVersionFromSolutionStack("64bit Windows Server Core 2019 v2.5.6 running IIS 10.0", _defaultVersion);
            Assert.Equal(new Version("2.5.6"), version);
        }

        [Theory]
        [InlineData("64bit Amazon Linux 2018.03 v4.14.4 running Node.js", false)]
        [InlineData("64bit legacy Windows Server 2019 v1.1.0 running IIS 10.0", false)]
        [InlineData("64bit legacy Windows Server 2019 v1.2.0 running IIS 10.0", true)]
        [InlineData("64bit legacy Windows Server 2019 v1.2.1 running IIS 10.0", true)]
        [InlineData("64bit legacy Windows Server 2019 v2.5.6 running IIS 10.0", true)]
        [InlineData("64bit OS v0.0.0 running DotNetCore", true)]
        [InlineData("64bit OS v0.0.0 running dotnetcore", true)]
        [InlineData("64bit OS v0.0.0 running .NET Core", true)]
        [InlineData("64bit OS v0.0.0 running .Net Core", true)]
        [InlineData("64bit OS v0.0.0 running .net core", true)]
        [InlineData("64bit Amazon Linux 2 v1.0.0 running .NET Core", true)]
        public void SolutionStackSupportsDotNetCore(string stackName, bool expectedResult)
        {
            Assert.Equal(expectedResult, SolutionStackUtils.SolutionStackSupportsDotNetCore(stackName));
        }

        [Theory]
        [InlineData("64bit Amazon Linux 2018.03 v4.14.4 running Node.js", false)]
        [InlineData("64bit legacy Windows Server 2019 v1.1.0 running IIS 10.0", true)]
        [InlineData("64bit legacy Windows Server 2019 v1.2.0 running IIS 10.0", true)]
        [InlineData("64bit legacy Windows Server 2019 v1.2.1 running IIS 10.0", true)]
        [InlineData("64bit legacy Windows Server 2019 v2.5.6 running IIS 10.0", true)]
        public void SolutionStackSupportsDotNetFramework(string stackName, bool expectedResult)
        {
            Assert.Equal(expectedResult, SolutionStackUtils.SolutionStackSupportsDotNetFramework(stackName));
        }

    }
}