using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    public static class TemplateContentType
    {
        public const string ContentType = "CloudFormationTemplate";
        public const string Extension = ".template";

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
        [FileExtension(TemplateContentType.Extension)]
        public static FileExtensionToContentTypeDefinition TemplateFileExtension { get; set; }
    }
}
