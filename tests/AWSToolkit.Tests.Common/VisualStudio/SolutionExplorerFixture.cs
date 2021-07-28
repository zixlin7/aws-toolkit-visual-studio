using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;

namespace Amazon.AWSToolkit.Tests.Common.VisualStudio
{
    /// <summary>
    /// Test fixture that helps mock code that queries the Solution Explorer's current selection
    /// </summary>
    public class SolutionExplorerFixture
    {
        // Param names have been kept consistent with the function's documentation
        // ReSharper disable IdentifierTypo
        // ReSharper disable InconsistentNaming
        private delegate void GetCurrentSelectionDelegate(out IntPtr ppHier,
            out uint pitemid,
            out IVsMultiItemSelect ppMIS,
            out IntPtr ppSC);

        private delegate void GetPropertyDelegate(
            uint itemid,
            int propid,
            out object pvar);
        // ReSharper restore IdentifierTypo
        // ReSharper restore InconsistentNaming

        private readonly Mock<IVsMonitorSelection> _monitorSelection = new Mock<IVsMonitorSelection>();
        private readonly Mock<IVsHierarchy> _hierarchy = new Mock<IVsHierarchy>();

        private object _currentSelection = null;
        private IntPtr _getCurrentSelectionHierarchy = IntPtr.Zero;
        private int _getCurrentSelectionReturnValue = VSConstants.S_OK;

        public IVsMonitorSelection MonitorSelection => _monitorSelection.Object;

        public SolutionExplorerFixture()
        {
            IntPtr hierarchyPtr, selectionContainerPtr;
            uint projectItemId;
            IVsMultiItemSelect mis;

            _monitorSelection.Setup(
                    m => m.GetCurrentSelection(out hierarchyPtr, out projectItemId, out mis, out selectionContainerPtr))
                // ReSharper disable IdentifierTypo
                // ReSharper disable InconsistentNaming
                .Callback(new GetCurrentSelectionDelegate((out IntPtr ppHier,
                    out uint pitemid,
                    out IVsMultiItemSelect ppMIS,
                    out IntPtr ppSC) =>
                    // ReSharper restore IdentifierTypo
                    // ReSharper restore InconsistentNaming
                {
                    ppHier = IntPtr.Zero;
                    ppHier = _getCurrentSelectionHierarchy;
                    pitemid = 0;
                    ppMIS = null;
                    ppSC = IntPtr.Zero;
                }))
                .Returns(() => _getCurrentSelectionReturnValue);


            object prjItemObject = null;
            _hierarchy.Setup(m => m.GetProperty(It.IsAny<uint>(), (int) __VSHPROPID.VSHPROPID_ExtObject, out prjItemObject))
                // ReSharper disable IdentifierTypo
                // ReSharper disable InconsistentNaming
                .Callback(new GetPropertyDelegate((uint itemid,
                    int propid,
                    out object pvar) =>
                    // ReSharper restore IdentifierTypo
                    // ReSharper restore InconsistentNaming
                {
                    pvar = _currentSelection;
                }))
                .Returns(VSConstants.S_OK);

        }

        public void CurrentSelectionFails()
        {
            _getCurrentSelectionHierarchy = Marshal.GetIUnknownForObject(_hierarchy.Object);
            _getCurrentSelectionReturnValue = VSConstants.S_FALSE;
        }

        public void ClearCurrentSelection()
        {
            _getCurrentSelectionHierarchy = IntPtr.Zero;
            _getCurrentSelectionReturnValue = VSConstants.S_OK;
        }

        public void SetCurrentSelection(object selection)
        {
            _getCurrentSelectionHierarchy = Marshal.GetIUnknownForObject(_hierarchy.Object);
            _getCurrentSelectionReturnValue = VSConstants.S_OK;
            _currentSelection = selection;
        }
    }
}
