using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Reflection;
using System.IO;

namespace Amazon.AWSToolkit.VisualStudio.Shared
{
    internal static class VSUtility
    {
        public static ServiceInterfaces.INetCoreProjectSupport NetCoreProjectSupport { get; private set; }

        private static bool _attemptLoadNetCoreSupportLibrary = true;

        private const string _netCoreSupportLibraryName = @"Plugins\AWSToolkitPackage.NetCoreSupport.dll";
        private const string _netCoreProjectSupportTypeName = "Amazon.AWSToolkit.VisualStudio.NetCoreProjectSupport";

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
                        if (projectTypeGuids.Length > 0)
                        {
                            foreach (var typeGuid in projectTypeGuids)
                            {
                                if (VSWebProjectInfo.IsWebProjectType(typeGuid))
                                {
                                    return new VSWebProjectInfo(projHier, QueryProjectIDGuid(projHier), typeGuid);
                                }
                            }
                        }
                        else
                        {
                            // CoreCLR projects have no type guid (at present) so we'll probe for using
                            // capability apis present in 2012+. 2015 editions and higher can load these 
                            // projects. We'll rely on inspection for the custom assembly to determine if 
                            // we should probe, to avoid having to inspect on multiple shell versions for
                            // this version of the toolkit.
                            if (_attemptLoadNetCoreSupportLibrary)
                            {
                                try
                                {
                                    var baseLocation = Assembly.GetExecutingAssembly().Location;
                                    var assemblyPath = Path.Combine(Path.GetDirectoryName(baseLocation),
                                                                    _netCoreSupportLibraryName);
                                    if (File.Exists(assemblyPath))
                                    {
                                        var assembly = Assembly.LoadFrom(assemblyPath);
                                        Type type = assembly.GetType(_netCoreProjectSupportTypeName);
                                        object typeInstance = Activator.CreateInstance(type);
                                        NetCoreProjectSupport = typeInstance as ServiceInterfaces.INetCoreProjectSupport;
                                    }

                                }
                                catch
                                { /* worth logging we didn't load? */ }
                                finally
                                {
                                    _attemptLoadNetCoreSupportLibrary = false;
                                }
                            }

                            if (NetCoreProjectSupport != null && NetCoreProjectSupport.IsNetCoreWebProject(hierarchyPtr))
                            {
                                return new VSWebProjectInfo(projHier, QueryProjectIDGuid(projHier), VSWebProjectInfo.guidAWSPrivateCoreCLRWebProject);
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
