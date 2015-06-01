using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Project.Automation;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using VsCommands = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;
using VSConstants = Microsoft.VisualStudio.VSConstants;

namespace Amazon.AWSToolkit.VisualStudio.CloudFormationEditor
{
    class TemplateFileNode : FileNode
    {
        internal TemplateFileNode(ProjectNode root, ProjectElement e)
			: base(root, e)
		{

		}

        /// <summary>
        /// This is what makes us have a special icon in the solution explorer
        /// </summary>
        public override int ImageIndex
        {
            get
            {
                if (this.FileName.ToLower().EndsWith(Amazon.AWSToolkit.CloudFormation.EditorExtensions.TemplateContentType.Extension))
                {
                    return CloudFormationTemplateProjectNode.templateFileNodeImageIndex;
                }
                return base.ImageIndex;
            }
        }
    }
}
