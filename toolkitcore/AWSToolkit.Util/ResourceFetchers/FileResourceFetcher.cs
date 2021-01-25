using System;
using System.IO;
using log4net;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    /// <summary>
    /// Retrieves data from a file on disk
    /// </summary>
    public class FileResourceFetcher : IResourceFetcher
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(FileResourceFetcher));

        /// <summary>
        /// Requests contents from a specified file on disk
        /// </summary>
        /// <returns>Stream of contents, null if there was an error or no contents were available.</returns>
        public virtual Stream Get(string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                return null;
            }

            try
            {
                Logger.Info($"Loading resource: {fullPath}");
                return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load resource: {fullPath}", e);
                return null;
            }
        }
    }
}
