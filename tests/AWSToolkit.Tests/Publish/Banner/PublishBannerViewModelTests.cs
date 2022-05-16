using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Publish.Banner;
using Amazon.AWSToolkit.Solutions;
using Amazon.AWSToolkit.Tests.Common.SampleData;
using Amazon.AWSToolkit.Tests.Common.Settings.Publish;
using Amazon.AWSToolkit.Util;

using Xunit;

namespace AWSToolkit.Tests.Publish.Banner
{
    public class PublishBannerViewModelTests
    {
        public static IEnumerable<object[]> TargetsWithExpectedShowBannerState = new List<object[]>
        {
            new object[] { SampleFrameworkNames.DotNet5, true },
            new object[] { SampleFrameworkNames.DotNetFramework472, false },
            new object[] { SampleFrameworkNames.Garbage, true },
        };

        [Theory]
        [MemberData(nameof(TargetsWithExpectedShowBannerState))]
        public void ShouldShowBannerBasedOnTargetFramework(FrameworkName targetFramework, bool expectedShowBanner)
        {
            // arrange.
            var project = CreateProjectWithTargetFramework(targetFramework);
            var toolkitContext = CreateToolkitContextWithProject(project);

            // act.
            var publishBanner = CreatePublishBannerWith(toolkitContext);

            // assert.
            Assert.Equal(expectedShowBanner, publishBanner.ShowBanner);
        }

        private Project CreateProjectWithTargetFramework(FrameworkName targetFramework)
        {
            return new Project("SampleProject", @"\my\path\SampleProject.csproj", Guid.NewGuid(), targetFramework);
        }

        private ToolkitContext CreateToolkitContextWithProject(Project project)
        {
            var toolkitHost = new ProjectToolkitShellProvider(project);
            var toolkitContext = CreateToolkitContext();
            toolkitContext.ToolkitHost = toolkitHost;
            return toolkitContext;
        }

        private ToolkitContext CreateToolkitContext()
        {
            var project = CreateProjectWithTargetFramework(SampleFrameworkNames.DotNet5);
            var toolkitHost = new ProjectToolkitShellProvider(project);
            return new ToolkitContext { ToolkitHost = toolkitHost, ToolkitHostInfo = ToolkitHosts.Vs2019 };
        }

        private PublishBannerViewModel CreatePublishBannerWith(ToolkitContext toolkitContext)
        {
            return new PublishBannerViewModel(toolkitContext, new InMemoryPublishSettingsRepository());
        }

        public static IEnumerable<object[]> VersionTestCases = new List<object[]>
        {
            new object[] { ToolkitHosts.Vs2019, true },
            new object[] { ToolkitHosts.Vs2017, false },
        };

        [Theory]
        [MemberData(nameof(VersionTestCases))]
        public void ShouldShowBannerBasedOnVisualStudioVersion(IToolkitHostInfo toolkitHostInfo, bool expected)
        {
            // arrange.
            var toolkitContext = CreateToolkitContextWith(toolkitHostInfo);

            // act.
            var publishBanner = CreatePublishBannerWith(toolkitContext);

            // assert.
            Assert.Equal(expected, publishBanner.ShowBanner);
        }

        private ToolkitContext CreateToolkitContextWith(IToolkitHostInfo toolkitHostInfo)
        {
            var toolkitHost = CreateToolkitContext();
            toolkitHost.ToolkitHostInfo = toolkitHostInfo;
            return toolkitHost;
        }
    }
}
