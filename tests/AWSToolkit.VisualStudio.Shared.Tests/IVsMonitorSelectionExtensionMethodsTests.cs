using Amazon.AWSToolkit.Tests.Common.VisualStudio;
using Amazon.AWSToolkit.VisualStudio.Shared;
using Xunit;

namespace Amazon.AWSToolkit.Tests.VisualStudio.Shared
{
    // ReSharper disable once InconsistentNaming
    public class IVsMonitorSelectionExtensionMethodsTests
    {
        private readonly SolutionExplorerFixture _solutionExplorerFixture = new SolutionExplorerFixture();

        public IVsMonitorSelectionExtensionMethodsTests()
        {

        }

        [Fact]
        public void GetCurrentSelectionVsHierarchy()
        {
            _solutionExplorerFixture.SetCurrentSelection(new {Name = "Foo"});
            var hierarchy = _solutionExplorerFixture.MonitorSelection.GetCurrentSelectionVsHierarchy(out uint projectItemId);
            Assert.NotNull(hierarchy);
        }

        [Fact]
        public void GetCurrentSelectionVsHierarchy_NoSelection()
        {
            _solutionExplorerFixture.ClearCurrentSelection();
            var hierarchy = _solutionExplorerFixture.MonitorSelection.GetCurrentSelectionVsHierarchy(out uint projectItemId);
            Assert.Null(hierarchy);
        }

        [Fact]
        public void GetCurrentSelectionVsHierarchy_Fails()
        {
            _solutionExplorerFixture.CurrentSelectionFails();
            var hierarchy = _solutionExplorerFixture.MonitorSelection.GetCurrentSelectionVsHierarchy(out uint projectItemId);
            Assert.Null(hierarchy);
        }

        [Fact]
        public void GetCurrentSelection()
        {
            var sampleObject = new {Name = "Bar"};
            _solutionExplorerFixture.SetCurrentSelection(sampleObject);
            var selection = _solutionExplorerFixture.MonitorSelection.GetCurrentSelection();
            Assert.True(object.ReferenceEquals(sampleObject, selection));
        }

        [Fact]
        public void GetCurrentSelection_Fails()
        {
            _solutionExplorerFixture.CurrentSelectionFails();
            var selection = _solutionExplorerFixture.MonitorSelection.GetCurrentSelection();
            Assert.Null(selection);
        }
    }
}
