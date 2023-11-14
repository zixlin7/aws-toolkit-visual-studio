using System.Collections.Generic;

using Newtonsoft.Json;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models
{
    /// <summary>
    /// Represents a single version of the language server
    /// </summary>
    public class LspVersion
    {
        /// <summary>
        /// Version of the lsp, in format x.x.x eg. 1.2.3
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ServerVersion { get; set; }
        /// <summary>
        /// When set to true indicates that this version should no longer be used and the contents may be empty
        /// </summary>
        public bool IsDelisted { get; set; } = false;
        /// <summary>
        /// List of Version Targets containing information about platform/architecture and associated files
        /// </summary>
        public IList<VersionTarget> Targets { get; set; }
    }
}
