using System.IO;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    /// <summary>
    /// Modular and compose-able component capable of retrieving data from a location.
    /// </summary>
    public interface IResourceFetcher
    {
        /// <summary>
        /// Requests contents from a location.
        /// The returned stream is not guaranteed to support seek.
        /// </summary>
        /// <returns>Stream of contents, null if there was an error or no contents were available.</returns>
        Stream Get(string path);
    }
}
