namespace AwsToolkit.VsSdk.Common.Settings
{
    /// <summary>
    /// Represents the cached etag value associated with the version manifest hosted in the url location
    /// </summary>
    public class ManifestCachedEtag
    {
        /// <summary>
        /// Url where the version manifest is located
        /// </summary>
        public string ManifestUrl { get; set; }

        /// <summary>
        /// Cached Etag of the version manifest hosted in the url location
        /// </summary>
        public string Etag { get; set; }
    }
}
