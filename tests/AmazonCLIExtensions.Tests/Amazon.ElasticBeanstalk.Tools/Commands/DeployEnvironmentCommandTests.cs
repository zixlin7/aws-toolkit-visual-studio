using System.Collections.Generic;
using System.Linq;

using Amazon.ElasticBeanstalk.Tools.Commands;

using Xunit;

namespace AmazonCLIExtensions.Tests.Amazon.ElasticBeanstalk.Tools.Commands
{
    public class DeployEnvironmentCommandTests
    {
        public class GetPublishOptionTestCase
        {
            public string InitialOptions { get; set; }
            public bool IsWindowsEnvironment { get; set; }
            public bool SelfContained { get; set; }
            public string Expected { get; set; }
        }

        public static IList<GetPublishOptionTestCase> TestCases = new List<GetPublishOptionTestCase>()
        {
            new GetPublishOptionTestCase()
            {
                InitialOptions = null,
                IsWindowsEnvironment = false,
                SelfContained = false,
                Expected = " --runtime linux-x64 --self-contained false"
            },
            new GetPublishOptionTestCase()
            {
                InitialOptions = "",
                IsWindowsEnvironment = false,
                SelfContained = false,
                Expected = " --runtime linux-x64 --self-contained false"
            },
            new GetPublishOptionTestCase()
            {
                InitialOptions = "",
                IsWindowsEnvironment = false,
                SelfContained = true,
                Expected = " --runtime linux-x64 --self-contained true"
            },
            new GetPublishOptionTestCase()
            {
                InitialOptions = "",
                IsWindowsEnvironment = true,
                SelfContained = false,
                Expected = " --runtime win-x64 --self-contained false"
            },
            new GetPublishOptionTestCase()
            {
                InitialOptions = "",
                IsWindowsEnvironment = true,
                SelfContained = true,
                Expected = " --runtime win-x64 --self-contained true"
            },
            new GetPublishOptionTestCase()
            {
                InitialOptions = "--self-contained true",
                IsWindowsEnvironment = true,
                SelfContained = false,
                Expected = "--self-contained true --runtime win-x64"
            },
            new GetPublishOptionTestCase()
            {
                InitialOptions = "--runtime linux-x64",
                IsWindowsEnvironment = true,
                SelfContained = true,
                Expected = "--runtime linux-x64 --self-contained true"
            }
        };

        public static IEnumerable<object[]> GetPublishOptionsTestCases = TestCases.Select(testCase => new object[] { testCase });

        [Theory]
        [MemberData(nameof(GetPublishOptionsTestCases))]
        public void ShouldGetPublishOptions(GetPublishOptionTestCase testCase)
        {
            // arrange.
            var command = CreateCommand();
            command.DeployEnvironmentOptions.PublishOptions = testCase.InitialOptions;
            command.DeployEnvironmentOptions.SelfContained = testCase.SelfContained;

            // act.
            var publishOptions = command.GetPublishOptions(testCase.IsWindowsEnvironment);

            // assert.
            Assert.Equal(testCase.Expected, publishOptions);
        }

        private DeployEnvironmentCommand CreateCommand()
        {
            return new DeployEnvironmentCommand(null, @"SampleApp\Project", null);
        }
    }
}
