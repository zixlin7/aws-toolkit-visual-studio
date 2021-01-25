using System.IO;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    /// <summary>
    /// Retrieves data from a file on disk relative to a base location
    /// </summary>
    public class RelativeFileResourceFetcher : FileResourceFetcher
    {
        private readonly string _basePath;

        public RelativeFileResourceFetcher(string basePath)
        {
            _basePath = basePath;
        }

        /// <summary>
        /// Requests contents from a specified file on disk, relative to this object's base path.
        /// </summary>
        /// <returns>Stream of contents, null if there was an error or no contents were available.</returns>
        public override Stream Get(string relativePath)
        {
            var path = Path.Combine(_basePath, relativePath);
            return base.Get(path);
        }
    }
}
