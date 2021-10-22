using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Settings.Publish;
using Amazon.AWSToolkit.Tests.Common.VisualStudio;
using Amazon.AWSToolkit.VisualStudio.Commands.Publishing;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.Publishing
{
    /// <summary>
    /// Test class that surfaces portions of PublishToAwsCommand for testing
    /// </summary>
    public class TestPublishToAwsCommand : PublishToAwsCommand
    {
        public TestPublishToAwsCommand(
            ToolkitContext toolkitContext,
            IAWSToolkitShellProvider toolkitShell,
            IPublishSettingsRepository publishSettingsRepository,
            DTE2 dte,
            IVsMonitorSelection monitorSelection,
            IVsSolution solution,
            IPublishToAws publishToAws)
            : base(toolkitContext, toolkitShell, publishSettingsRepository, dte, monitorSelection, solution, publishToAws)
        {
        }

        /// <summary>
        /// Exposes BeforeQueryStatus to test when commands are made
        /// visible in VS.
        /// </summary>
        public void ExposedBeforeQueryStatus(OleMenuCommand menuCommand)
        {
            BeforeQueryStatus(menuCommand, EventArgs.Empty);
        }
    }

    [Collection(UIThreadFixtureCollection.CollectionName)]
    public class PublishToAwsCommandTests
    {
        public class SetupState
        {
            public bool OptedInToPublishToAws;
            public FrameworkName TargetedFramework;
            public bool ExpectedMenuVisibility;
        }

        private const string SampleProjectName = "SomeProjectName";
        private static readonly FrameworkName SampleDotNetFrameworkTarget =
            new FrameworkName(".NETFramework,Version=v4.7.2");
        private static readonly FrameworkName SampleDotNetCoreTarget =
            new FrameworkName(".NETCoreApp,Version=v5.0");

        public static IEnumerable<object[]> SetupStates = new List<object[]>
        {
            new object[]
            {
                new SetupState
                {
                    OptedInToPublishToAws = true,
                    TargetedFramework = SampleDotNetCoreTarget,
                    ExpectedMenuVisibility = true,
                }
            },
            new object[]
            {
                new SetupState
                {
                    OptedInToPublishToAws = true,
                    TargetedFramework = SampleDotNetFrameworkTarget,
                    ExpectedMenuVisibility = false,
                }
            },
            new object[]
            {
                new SetupState
                {
                    OptedInToPublishToAws = false,
                    TargetedFramework = SampleDotNetCoreTarget,
                    ExpectedMenuVisibility = false,
                }
            },
            new object[]
            {
                new SetupState
                {
                    OptedInToPublishToAws = false,
                    TargetedFramework = SampleDotNetFrameworkTarget,
                    ExpectedMenuVisibility = false,
                }
            },
        };

        private readonly Mock<DTE2> _dte = new Mock<DTE2>();
        private readonly Mock<Solution> _solution = new Mock<Solution>();
        private readonly Mock<SelectedItems> _selectedItems = new Mock<SelectedItems>();
        private readonly Mock<Project> _sampleProject;
        private readonly Mock<IPublishToAws> _publishToAws = new Mock<IPublishToAws>();
        private readonly Mock<IAWSToolkitShellProvider> _toolkitShell = new Mock<IAWSToolkitShellProvider>();
        private readonly PublishSettings _publishSettings = new PublishSettings();
        private readonly IPublishSettingsRepository _publishSettingsRepository = new InMemoryPublishSettingsRepository();

        private readonly SolutionExplorerFixture _solutionExplorer = new SolutionExplorerFixture();

        private readonly TestPublishToAwsCommand _sut;
        private readonly OleMenuCommand _menuCommand;

        public PublishToAwsCommandTests()
        {
            _publishSettings.ShowPublishMenu = true;
            _publishSettingsRepository.Save(_publishSettings);

            _sampleProject = new Mock<Project>();
            _sampleProject.SetupGet(m => m.Name).Returns(SampleProjectName);

            _solution.SetupGet(m => m.IsOpen).Returns(true);
            _dte.SetupGet(m => m.Solution).Returns(_solution.Object);

            _selectedItems.SetupGet(m => m.MultiSelect).Returns(false);
            _selectedItems.SetupGet(m => m.Count).Returns(1);
            _dte.SetupGet(m => m.SelectedItems).Returns(_selectedItems.Object);

            _toolkitShell.Setup(mock => mock.ExecuteOnUIThread<bool>(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(func => func().Result);

            _sut = new TestPublishToAwsCommand(null, _toolkitShell.Object, _publishSettingsRepository,
                _dte.Object, _solutionExplorer.MonitorSelection, _solutionExplorer.Solution,
                _publishToAws.Object);
            _menuCommand = new OleMenuCommand(null, null);
        }

        [Theory]
        [MemberData(nameof(SetupStates))]
        public void BeforeQueryStatus_SolutionExplorerSelectsProject(SetupState setupState)
        {
            _publishSettings.ShowPublishMenu = setupState.OptedInToPublishToAws;
            _solutionExplorer.SetCurrentSelection(_sampleProject.Object);
            _solutionExplorer.SetProjectTargetFramework(setupState.TargetedFramework);

            _sut.ExposedBeforeQueryStatus(_menuCommand);

            Assert.Equal(setupState.ExpectedMenuVisibility, _menuCommand.Visible);
            if (setupState.ExpectedMenuVisibility)
            {
                Assert.Equal("Publish SomeProjectName to AWS (Preview feature)...", _menuCommand.Text);
            }
        }

        [Fact]
        public void BeforeQueryStatus_NoSolutionOpen()
        {
            _solution.SetupGet(m => m.IsOpen).Returns(false);

            _sut.ExposedBeforeQueryStatus(_menuCommand);

            Assert.False(_menuCommand.Visible);
        }

        [Fact]
        public void BeforeQueryStatus_MultipleItemsSelected()
        {
            _selectedItems.SetupGet(m => m.MultiSelect).Returns(true);

            _sut.ExposedBeforeQueryStatus(_menuCommand);

            Assert.False(_menuCommand.Visible);
        }

        [Fact]
        public void BeforeQueryStatus_NoItemsSelected()
        {
            _selectedItems.SetupGet(m => m.Count).Returns(0);

            _sut.ExposedBeforeQueryStatus(_menuCommand);

            Assert.False(_menuCommand.Visible);
        }

        [Fact]
        public void BeforeQueryStatus_SolutionExplorerSelectsNothing()
        {
            _solutionExplorer.ClearCurrentSelection();

            _sut.ExposedBeforeQueryStatus(_menuCommand);

            Assert.False(_menuCommand.Visible);
        }

        [Fact]
        public void BeforeQueryStatus_SolutionExplorerSelectsNonProject()
        {
            var projectItem = new Mock<ProjectItem>();
            _solutionExplorer.SetCurrentSelection(projectItem.Object);

            _sut.ExposedBeforeQueryStatus(_menuCommand);

            Assert.False(_menuCommand.Visible);
        }

        [Theory]
        [MemberData(nameof(SetupStates))]
        public void BeforeQueryStatus_SolutionExplorerSelectsProjectItemWithinProject(SetupState setupState)
        {
            _publishSettings.ShowPublishMenu = setupState.OptedInToPublishToAws;
            var projectItem = new Mock<ProjectItem>();
            projectItem.SetupGet(m => m.ContainingProject).Returns(_sampleProject.Object);

            _solutionExplorer.SetCurrentSelection(projectItem.Object);
            _solutionExplorer.SetProjectTargetFramework(setupState.TargetedFramework);

            _sut.ExposedBeforeQueryStatus(_menuCommand);

            Assert.Equal(setupState.ExpectedMenuVisibility, _menuCommand.Visible);
            if (setupState.ExpectedMenuVisibility)
            {
                Assert.Equal("Publish SomeProjectName to AWS (Preview feature)...", _menuCommand.Text);
            }
        }
    }
}
