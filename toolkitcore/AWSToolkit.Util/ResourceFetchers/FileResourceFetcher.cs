using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using log4net;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    /// <summary>
    /// Retrieves data from a file on disk
    /// </summary>
    public class FileResourceFetcher : IResourceFetcher
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(FileResourceFetcher));

        /// <summary>
        /// Requests contents from a specified file on disk
        /// </summary>
        /// <returns>Stream of contents, null if there was an error or no contents were available.</returns>
        public virtual Task<Stream> GetAsync(string fullPath, CancellationToken token = default)
        {
            if (!File.Exists(fullPath))
            {
                return Task.FromResult<Stream>(null);
            }

            try
            {
                _logger.Info($"Loading resource: {fullPath}");
                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                return Task.FromResult<Stream>(fileStream);
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to load resource: {fullPath}", e);
                return null;
            }
        }
    }
}
