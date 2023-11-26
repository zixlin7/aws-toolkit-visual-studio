using System.Collections.Generic;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models
{
    /// <summary>
    /// Indicates platform, architecture and associated files for a specific LSP version <see cref="LspVersion"/>
    /// </summary>
    public class VersionTarget
    {
        public string Platform { get; set; }
        public string Arch { get; set; }
        public IList<TargetContent> Contents { get; set; }
    }
}
