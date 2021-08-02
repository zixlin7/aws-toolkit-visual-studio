using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

using Amazon.AWSToolkit;

using log4net;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Constants = Amazon.AWSToolkit.Constants;

namespace Amazon.AwsToolkit.VsSdk.Common
{
    public static class VSUtility
    {

        // CoreCLR projects have no type guid (at present) so we'll probe for using
        // capability apis present in 2012+.
        private const string _dotNetCoreWebCapability = "DotNetCoreWeb";

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(VSUtility));

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

        public static bool IsNETCoreDockerProject
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
                        if (projHier != null && projHier.IsCapabilityMatch("CrossPlatformExecutable"))
                        {
                            Object prjItemObject = null;
                            projHier.GetProperty(projectItemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out prjItemObject);

                            var prjItem = prjItemObject as EnvDTE.Project;
                            if (prjItem == null)
                                return false;

                            var dockerFilePath = Path.Combine(Path.GetDirectoryName(prjItem.FullName), "Dockerfile");
                            if (File.Exists(dockerFilePath))
                                return true;
                        }
                    }
                }
                catch (Exception) { }

                return false;
            }
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
                            if (projHier != null && projHier.IsCapabilityMatch(_dotNetCoreWebCapability))
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

        public static string GetSelectedItemFullPath()
        {
            var item = GetSelectedProjectItem();
            if (item == null)
                return null;

            if (item.FileCount == 0)
                return null;
            
            // Solution items act differently then project items. The file names collection is 1 based.
            // Because the behavior of getting solution item file path is so weird added 
            // extra try/catch and logging around that behavior.
            if (item.Kind == Constants.VS_SOLUTION_ITEM_KIND_GUID)
            {
                try
                {
                    return item.FileNames[1];
                }
                catch(Exception e)
                {
                    LOGGER.Warn($"Failed to get full file path for solution item.", e);
                    return null;
                }
            }
            else
            {
                return item.FileNames[0];
            }
        }

        public static bool SelectedFileMatchesName(string filename)
        {
            var prjItem = GetSelectedProjectItem();
            if (prjItem == null || prjItem.Name == null)
                return false;

            return string.Equals(filename, prjItem.Name);
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
            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
            return monitorSelection.GetCurrentSelection() as EnvDTE.ProjectItem;
        }

        public static EnvDTE.Project GetSelectedProject()
        {
            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
            return monitorSelection.GetCurrentSelection() as EnvDTE.Project;
        }

        public static bool IsLambdaDotnetProject
        {
            get
            {
                try
                {
                    var project = GetSelectedProject();
                    if (project == null)
                        return false;

                    var projectContent = File.ReadAllText(project.FileName);
                    if (projectContent.Contains("\"Amazon.Lambda.Tools\""))
                        return true;

                    if (ProjectFileUtilities.IsProjectType(projectContent, ProjectFileUtilities.LAMBDA_PROJECT_TYPE_ID))
                        return true;


                    var projectJsonPath = Path.Combine(new FileInfo(project.FileName).DirectoryName, "project.json");
                    if(File.Exists(projectJsonPath))
                    {
                        var content = File.ReadAllText(projectJsonPath);
                        if (content.Contains("\"Amazon.Lambda.Tools\""))
                            return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }


        public static IList<string> GetSelectedNetCoreProjectFrameworks()
        {
            var frameworks = new List<string>();

            try
            {
                var project = VSUtility.GetSelectedProject();
                var projectContent = File.ReadAllText(project.FullName);

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(projectContent);

                var singleFrameworkNode = xmlDoc.SelectSingleNode("//PropertyGroup/TargetFramework");
                if(singleFrameworkNode != null && !string.IsNullOrEmpty(singleFrameworkNode.InnerText))
                {
                    frameworks.Add(singleFrameworkNode.InnerText);
                }

                if (frameworks.Count == 0)
                {
                    var multipleFrameworkNode = xmlDoc.SelectSingleNode("//PropertyGroup/TargetFrameworks");
                    if (multipleFrameworkNode != null && !string.IsNullOrEmpty(multipleFrameworkNode.InnerText))
                    {
                        foreach (var framework in multipleFrameworkNode.InnerText.Split(';'))
                        {
                            frameworks.Add(framework);
                        }
                    }
                }
            }
            catch
            {
                
            }

            return frameworks;
        }

        public static string GetSelectedProjectLocation()
        {
            var project = GetSelectedProject();
            if (project == null)
                return null;

            return new FileInfo(project.FileName).DirectoryName;
        }


        public static IVsHierarchy GetCurrentVSHierarchySelection(out uint projectItemId)
        {
            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
            return monitorSelection.GetCurrentSelectionVsHierarchy(out projectItemId);
        }

        public static IVsProject GetVsProjectForHierarchyNode(IVsHierarchy hierachyNode)
        {
            IVsProject vsProject = null;
            if (hierachyNode is IVsProject)
                vsProject = hierachyNode as IVsProject;
            else if (hierachyNode is FolderNode)
            {
                IVsHierarchy node;
                uint projectItemId;
                ((FolderNode)hierachyNode).NodeProperties.GetProjectItem(out node, out projectItemId);
                vsProject = node as IVsProject;
            }

            return vsProject;
        }
    }
}
