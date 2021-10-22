using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Amazon.AWSToolkit.AwsServices;
using Amazon.AWSToolkit.CloudFormation.EditorExtensions;

using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using VsCommands2K = Microsoft.VisualStudio.VSConstants.VSStd2KCmdID;
using VSConstants = Microsoft.VisualStudio.VSConstants;
using VSProjectUtilities = Microsoft.VisualStudio.Project.Utilities;

namespace Amazon.AWSToolkit.VisualStudio.Editors.CloudFormation
{
    public class CloudFormationTemplateProjectNode : ProjectNode, IErrorListReporter, IVsDeferredSaveProject
    {
        private static ImageList projectNodeImageList;
        internal static int cloudFormationProjectImageIndex;

        private static ImageList templateFileNodeImageList;
        internal static int templateFileNodeImageIndex;

        private AWSToolkitPackage package;


        static CloudFormationTemplateProjectNode()
        {
            projectNodeImageList = VSProjectUtilities.GetImageList(typeof(CloudFormationTemplateProjectNode).Assembly.GetManifestResourceStream("Amazon.AWSToolkit.VisualStudio.Resources.CloudFormationTemplateProjectNode.png"));
            templateFileNodeImageList = VSProjectUtilities.GetImageList(typeof(CloudFormationTemplateProjectNode).Assembly.GetManifestResourceStream("Amazon.AWSToolkit.VisualStudio.Resources.CloudFormationTemplateNode.png"));
        }

        public CloudFormationTemplateProjectNode(AWSToolkitPackage package)
        {
            this.package = package;
            this.CanProjectDeleteItems = true;


            cloudFormationProjectImageIndex = this.ImageHandler.ImageList.Images.Count;
            foreach (Image img in projectNodeImageList.Images)
                this.ImageHandler.AddImage(img);

            templateFileNodeImageIndex = this.ImageHandler.ImageList.Images.Count;
            foreach (Image img in templateFileNodeImageList.Images)
                this.ImageHandler.AddImage(img);
        }

        public override int ImageIndex => cloudFormationProjectImageIndex;

        public int SaveProjectToLocation(string pszProjectFilename)
        {
            string oldRootDirectory = Path.GetDirectoryName(this.Url);
            var retCode = this.SaveAs(pszProjectFilename);
            if(retCode != VSConstants.S_OK)
                return retCode;

            var rootDir = Path.GetDirectoryName(pszProjectFilename);

            var dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            var projectFirstChild = this.FirstChild;

            if (projectFirstChild != null)
            {
                this.package.JoinableTaskFactory.Run(async () =>
                {
                    await this.package.JoinableTaskFactory.SwitchToMainThreadAsync();
                    Action<HierarchyNode> copyNodes = null;
                    copyNodes = node =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        while (node != null)
                        {
                            if (node is FileNode)
                            {
                                Object prjItemObject = null;
                                
                                this.GetProperty(node.ID, (int)__VSHPROPID.VSHPROPID_ExtObject, out prjItemObject);

                                var prjItem = prjItemObject as EnvDTE.ProjectItem;
                                if (prjItem == null)
                                    continue;

                                var fileName = node.Url.Substring(oldRootDirectory.Length + 1);
                                var fullPath = Path.Combine(rootDir, fileName);
                                prjItem.SaveAs(fullPath);
                            }

                            var firstChild = node.FirstChild;
                            if (firstChild != null)
                                copyNodes(firstChild);

                            node = node.NextSibling;
                        }
                    };

                    copyNodes(projectFirstChild);
                });
            }

            return VSConstants.S_OK;
        }

        public override Guid ProjectGuid => GuidList.guidCloudFormationTemplateProjectFactory;

        public override string ProjectType => "AWS CloudFormation";

        public override void AddFileFromTemplate(string source, string target)
        {
            if (!File.Exists(target))
            {
                this.FileTemplateProcessor.UntokenFile(source, target);
            }
            this.FileTemplateProcessor.Reset();
        }

        /// <summary>
        /// Disable references
        /// </summary>
        /// <returns></returns>
        protected override ReferenceContainerNode CreateReferenceContainerNode()
        {
            return null;
        }

        /// <summary>
        /// Disable references
        /// </summary>
        /// <returns></returns>
        public override int AddProjectReference()
        {
            return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        /// <summary>
        /// Override so our custom TemplateFileNode will be created instead
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override FileNode CreateFileNode(ProjectElement item)
        {
            if (item.GetMetadata("Include").EndsWith(ToolkitFileTypes.CloudFormationTemplateExtension))
            {
                return new TemplateFileNode(this, item);
            }

            return new FileNode(this, item);
        }

        /// <summary>
        /// This says ".template" files are code files so that item type will be set to compile
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public override bool IsCodeFile(string fileName)
        {
            return fileName.EndsWith(ToolkitFileTypes.CloudFormationTemplateExtension);
        }

        /// <summary>
        /// Use this to turn off references
        /// </summary>
        /// <param name="guidCmdGroup"></param>
        /// <param name="cmd"></param>
        /// <param name="pCmdText"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected override int QueryStatusOnNode(Guid guidCmdGroup, uint cmd, IntPtr pCmdText, ref QueryStatusResult result)
        {
            if (guidCmdGroup == Microsoft.VisualStudio.Project.VsMenus.guidStandardCommandSet2K)
            {
                if ((VsCommands2K)cmd == VsCommands2K.ADDREFERENCE)
                {
                    result = QueryStatusResult.INVISIBLE | QueryStatusResult.NOTSUPPORTED;
                    return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
                }
            }
            return base.QueryStatusOnNode(guidCmdGroup, cmd, pCmdText, ref result);
        }

        protected override int ExecCommandOnNode(Guid cmdGroup, uint cmd, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return base.ExecCommandOnNode(cmdGroup, cmd, nCmdexecopt, pvaIn, pvaOut);
        }

        /// <summary>
        /// This turns off bring up the property page and the default property grid will come up instead
        /// </summary>
        /// <returns></returns>
        protected override Guid[] GetConfigurationIndependentPropertyPages()
        {
            return new Guid[0];
        }

        /// <summary>
        /// This basically lets us delete from disk as well as from project
        /// </summary>
        /// <param name="deleteOperation"></param>
        /// <returns></returns>
        protected override bool CanDeleteItem(__VSDELETEITEMOPERATION deleteOperation)
        {
            if (deleteOperation == __VSDELETEITEMOPERATION.DELITEMOP_DeleteFromStorage || 
                deleteOperation == __VSDELETEITEMOPERATION.DELITEMOP_RemoveFromProject)
            {
                return true;
            }

            return false;
        }

        Microsoft.VisualStudio.Shell.TaskProvider.TaskCollection IErrorListReporter.Tasks => this.TaskProvider.Tasks;

#if VS2022_OR_LATER
        bool IErrorListReporter.Navigate(TaskListItem task, Guid logicalView)
#else
        bool IErrorListReporter.Navigate(Microsoft.VisualStudio.Shell.Task task, Guid logicalView)
        #endif
        {            
            return TaskProvider.Navigate(task, logicalView);
        }

        void IErrorListReporter.ResumeRefresh()
        {
            TaskProvider.ResumeRefresh();
        }

        void IErrorListReporter.SuspendRefresh()
        {
            TaskProvider.SuspendRefresh();
        }
    }
}
