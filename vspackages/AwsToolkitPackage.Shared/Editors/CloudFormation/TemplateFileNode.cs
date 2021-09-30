using Amazon.AWSToolkit.AwsServices;

using Microsoft.VisualStudio.Project;

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
                if (this.FileName.ToLower().EndsWith(ToolkitFileTypes.CloudFormationTemplateExtension))
                {
                    return CloudFormationTemplateProjectNode.templateFileNodeImageIndex;
                }
                return base.ImageIndex;
            }
        }
    }
}
