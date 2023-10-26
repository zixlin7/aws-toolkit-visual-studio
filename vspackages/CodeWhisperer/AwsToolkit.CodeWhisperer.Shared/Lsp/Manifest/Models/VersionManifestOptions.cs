using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models
{
    public class VersionManifestOptions
    {
        /// <summary>
        /// Each toolkit release is expected to be compatible per major version (eg: 0.x, 1.x, ...) of the version manifest schema
        /// </summary>
        public int MajorVersion { get; set; } = 0;

        /// <summary>
        /// Specifies the name of the file that represents the LSP binary
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Specifies the name of the Language Server
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// CloudFront location to fetch manifest from
        /// </summary>
        public string CloudFrontUrl { get; set; } = string.Empty;

        public ToolkitContext ToolkitContext { get; set; }
    }
}
