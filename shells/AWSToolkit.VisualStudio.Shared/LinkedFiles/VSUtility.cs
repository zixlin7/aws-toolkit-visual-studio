using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Shared
{
    internal static class VSUtility
    {
        public static string QueryProjectIDGuid(IVsHierarchy projectHier)
        {
            Guid projectGuid;
            // not all project types implement __VSHPROPID.VSHPROPID_ProjectIDGuid, so get it
            // via solution
            var vsSolution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            vsSolution.GetGuidOfProject(projectHier, out projectGuid);
            return projectGuid.ToString("B");
        }

        public static string QueryProjectIDGuid(EnvDTE.Project project)
        {
            Guid projectGuid;
            // not all project types implement __VSHPROPID.VSHPROPID_ProjectIDGuid, so get it
            // via solution
            var vsSolution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            IVsHierarchy hierarchy;
            vsSolution.GetProjectOfUniqueName(project.FullName, out hierarchy);
            vsSolution.GetGuidOfProject(hierarchy, out projectGuid);
            return projectGuid.ToString("B");
        }

        /// <summary>
        /// Return wrapper around selected web project, provided it is the only selected
        /// item and is a root object....
        /// </summary>
        public static VSWebProjectInfo SelectedWebProject
        {
            get
            {
                try
                {
                    IntPtr hierarchyPtr, selectionContainerPtr;
                    uint projectItemId;
                    IVsMultiItemSelect mis;
                    var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
                    monitorSelection.GetCurrentSelection(out hierarchyPtr, out projectItemId, out mis, out selectionContainerPtr);

                    if (hierarchyPtr != null && projectItemId == VSConstants.VSITEMID_ROOT)
                    {
                        var projHier = Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;

                        var projectTypeGuids = VSWebProjectInfo.QueryProjectTypeGuids(projHier);
                        foreach (var typeGuid in projectTypeGuids)
                        {
                            if (VSWebProjectInfo.IsWebProjectType(typeGuid))
                            {
                                return new VSWebProjectInfo(projHier, QueryProjectIDGuid(projHier), typeGuid);
                            }
                        }
                    }
                }
                catch (Exception) { }

                return null;
            }
        }

        public static bool SelectedFileHasExtension(string extension)
        {
            var prjItem = GetSelectedProjectItem();
            if (prjItem == null || prjItem.Name == null)
                return false;
            return prjItem.Name.EndsWith(extension);
        }

        public static EnvDTE.ProjectItem GetSelectedProjectItem()
        {
            IntPtr hierarchyPtr, selectionContainerPtr;
            uint projectItemId;
            IVsMultiItemSelect mis;
            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
            if (monitorSelection.GetCurrentSelection(out hierarchyPtr, out projectItemId, out mis, out selectionContainerPtr) == VSConstants.S_OK
                && hierarchyPtr != IntPtr.Zero)
            {
                var projHier = Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
                Object prjItemObject = null;
                projHier.GetProperty(projectItemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out prjItemObject);

                var prjItem = prjItemObject as EnvDTE.ProjectItem;
                if (prjItem != null)
                    return prjItem;
            }

            return null;
        }

        public static EnvDTE.Project GetSelectedProject()
        {
            IntPtr hierarchyPtr, selectionContainerPtr;
            uint projectItemId;
            IVsMultiItemSelect mis;
            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
            if (monitorSelection.GetCurrentSelection(out hierarchyPtr, out projectItemId, out mis, out selectionContainerPtr) == VSConstants.S_OK
                && hierarchyPtr != IntPtr.Zero)
            {
                var projHier = Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
                Object prjItemObject = null;
                projHier.GetProperty(projectItemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out prjItemObject);

                var prjItem = prjItemObject as EnvDTE.Project;
                if (prjItem == null)
                    return null;
                return prjItem;
            }

            return null;
        }

        public static IVsHierarchy GetCurrentVSHierarchySelection(out uint projectItemId)
        {
            IntPtr hierarchyPtr, selectionContainerPtr;
            IVsMultiItemSelect mis;
            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
            if (monitorSelection.GetCurrentSelection(out hierarchyPtr,
                                                     out projectItemId,
                                                     out mis,
                                                     out selectionContainerPtr) == VSConstants.S_OK && hierarchyPtr != IntPtr.Zero)
                return Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;

            return null;
        }
    }
}
