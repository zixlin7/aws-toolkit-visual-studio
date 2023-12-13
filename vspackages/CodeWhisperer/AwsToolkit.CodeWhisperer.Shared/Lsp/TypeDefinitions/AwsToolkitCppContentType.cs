using System.ComponentModel.Composition;

using Community.VisualStudio.Toolkit;

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.TypeDefinitions
{
    // This is used to associate our language server with C and C++ files
    // File handling and functionality relating to C and C++ is reserved by VS.
    // This is the only way we know so far that will activate our LSP and uses it with relevant files.
    // See FAQ: https://learn.microsoft.com/en-us/visualstudio/extensibility/adding-an-lsp-extension?view=vs-2022#faq
    internal static class AwsToolkitCppContentType
    {
        // This also becomes the languageId provided to the language server
        // The drawback is that this becomes the file's content type, but we don't have a workaround to this presently.
        public const string ContentTypeName = "aws-toolkit-c-cpp";

        [Export]
        [Name(ContentTypeName)]
        // "Deriving" from the VS content type for C/C++ maintains existing functionality, and adds our own into VS
        [BaseDefinition(ContentTypes.CPlusPlus)]
        // Content type must derive from CodeRemoteContentTypeName
        // https://learn.microsoft.com/en-us/visualstudio/extensibility/adding-an-lsp-extension?view=vs-2022#content-type-definition
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition _contentTypeDefinition;

        // Below we associate file extensions with the "behavior" attributed above to ContentTypeName

        [Export]
        [FileExtension(".cpp")]
        [ContentType(ContentTypeName)]
        internal static FileExtensionToContentTypeDefinition _cppFileExtensionDefinition;

        [Export]
        [FileExtension(".hpp")]
        [ContentType(ContentTypeName)]
        internal static FileExtensionToContentTypeDefinition _hppFileExtensionDefinition;

        [Export]
        [FileExtension(".c")]
        [ContentType(ContentTypeName)]
        internal static FileExtensionToContentTypeDefinition _cFileExtensionDefinition;

        [Export]
        [FileExtension(".h")]
        [ContentType(ContentTypeName)]
        internal static FileExtensionToContentTypeDefinition _hFileExtensionDefinition;
    }
}
