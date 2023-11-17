namespace AwsToolkit.VsSdk.Common.Settings
{
    /// <summary>
    /// Represents a dismissed notification regarding deprecation of a version manifest
    /// </summary>
    public class DismissedManifestDeprecation
    {
        /// <summary>
        ///  Major version of the manifest schema
        /// </summary>
        public int SchemaMajorVersion { get; set; }

        /// <summary>
        /// Url where the version manifest is located
        /// </summary>
        public string ManifestUrl { get; set; }
    }
}
