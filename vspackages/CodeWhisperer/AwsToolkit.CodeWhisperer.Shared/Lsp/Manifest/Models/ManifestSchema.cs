using System.Collections.Generic;

using Newtonsoft.Json;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models
{
    // TODO: IDE-11637 Add validation logic for manifest schema eg. platform types allowed
    /// <summary>
    /// Describes the LSP Version Manifest schema used by the toolkit to look up and download the Language server
    /// </summary>
    public class ManifestSchema
    {
        /// <summary>
        /// Schema format version
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ManifestSchemaVersion { get; set; }
        /// <summary>
        /// Unique ID for the system represented by the manifest
        /// </summary>
        public string ArtifactId { get; set; }
        /// <summary>
        /// Description of the artifact
        /// </summary>
        public string ArtifactDescription { get; set; }
        // TODO: IDE-11630  Introduce handling for manifest schema deprecation
        public bool IsManifestDeprecated { get; set; } = false;
        /// <summary>
        /// List of Lsp Versions
        /// </summary>
        public IList<LspVersion> Versions { get; set; }
    }
}
