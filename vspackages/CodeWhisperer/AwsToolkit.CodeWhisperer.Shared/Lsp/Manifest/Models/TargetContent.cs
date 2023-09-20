using System.Collections.Generic;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models
{
    /// <summary>
    /// Represents a file associated with the <see cref="VersionTarget"/>
    /// </summary>
    public class TargetContent
    {
        public string FileName { get; set; }
        public string Url { get; set; }
        /// <summary>
        /// List containing one or more computed hashes for the file
        /// each entry is represented in the format "<hash-strategy>:<hash-value>" eg. "sha384:abcd"
        /// </summary>
        public IList<string> Hashes { get; set; }
        /// <summary>
        /// Size in bytes of the payload
        /// </summary>
        public int Bytes { get; set; }
    }
}
