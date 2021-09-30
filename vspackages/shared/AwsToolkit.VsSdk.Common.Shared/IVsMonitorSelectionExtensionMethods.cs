using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AwsToolkit.VsSdk.Common
{
    /// <summary>
    /// Convenience wrappers relating to the Solution Explorer's selected item.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IVsMonitorSelectionExtensionMethods
    {
        /// <summary>
        /// Retrieve the VsHierarchy of the Solution Explorer's current
        /// selected item.
        ///
        /// Does not support when multiple items are selected.
        /// </summary>
        public static IVsHierarchy GetCurrentSelectionVsHierarchy(
            this IVsMonitorSelection monitorSelection,
            out uint projectItemId)
        {
            projectItemId = 0;

            try
            {
                var getSelectionResult = monitorSelection.GetCurrentSelection(
                    out var hierarchyPtr,
                    out projectItemId,
                    out _,
                    out _);

                if (getSelectionResult == VSConstants.S_OK && hierarchyPtr != IntPtr.Zero)
                {
                    return Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
                }
            }
            catch (Exception e)
            {
                // Do not log spam -- this could be inside a query loop
                Debug.Assert(false, $"Failure getting Solution Explorer selection: {e.Message}");
            }

            return null;
        }

        /// <summary>
        /// Retrieve the currently selected object in the Solution Explorer.
        /// This is often (but not always) a Project or ProjectItem.
        ///
        /// Does not support when multiple items are selected.
        /// </summary>
        public static object GetCurrentSelection(this IVsMonitorSelection monitorSelection)
        {
            try
            {
                var hierarchy = monitorSelection.GetCurrentSelectionVsHierarchy(out uint projectItemId);
                if (hierarchy == null)
                {
                    return null;
                }

                var result = hierarchy.GetProperty(projectItemId,
                    (int) __VSHPROPID.VSHPROPID_ExtObject, out var projectItem);

                if (result != VSConstants.S_OK)
                {
                    return null;
                }

                return projectItem;
            }
            catch (Exception e)
            {
                // Do not log spam -- this could be inside a query loop
                Debug.Assert(false, $"Failure getting Solution Explorer selection: {e.Message}");
                return null;
            }
        }
    }
}
