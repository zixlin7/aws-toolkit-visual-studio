using Microsoft.VisualStudio.Project;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using VsCommands = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;

namespace Amazon.AWSToolkit.VisualStudio.Editors.CloudFormation
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
