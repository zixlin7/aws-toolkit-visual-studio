using System.ComponentModel.Composition;

using Amazon.AWSToolkit.AwsServices;

using Microsoft.VisualStudio.Utilities;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    public static class TemplateContentType
    {
        internal const string ContentType = "CloudFormationTemplate";

        /// <summary>
        /// Exports the Template content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(TemplateContentType.ContentType)]
        [BaseDefinition("code")]
        public static ContentTypeDefinition TemplateContentTypeDefinition { get; set; }

        /// <summary>
        /// Exports the Template file extension
        /// </summary>
        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(TemplateContentType.ContentType)]
        [FileExtension(ToolkitFileTypes.CloudFormationTemplateExtension)]
        public static FileExtensionToContentTypeDefinition TemplateFileExtension { get; set; }
    }
}
