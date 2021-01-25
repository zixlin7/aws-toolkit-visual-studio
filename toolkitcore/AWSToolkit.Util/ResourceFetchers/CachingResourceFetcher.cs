﻿using System;
using System.IO;
using log4net;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    /// <summary>
    /// This fetcher caches to a specified location if the provided fetcher is successful
    /// in retrieving data.
    /// </summary>
    public class CachingResourceFetcher : IResourceFetcher
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(CachingResourceFetcher));

        public delegate string GetCacheFullPath(string path);

        private readonly IResourceFetcher _fetcher;
        private readonly GetCacheFullPath _getCacheFullPath;

        /// <summary>
        /// Writes fetched resource to a cache, returns stream from the cache location.
        /// </summary>
        /// <param name="fetcher">Fetcher to retrieve data with</param>
        /// <param name="fnCacheFullPath">resolves the "Get" path into a full cache location path to write the contents to</param>
        public CachingResourceFetcher(IResourceFetcher fetcher, GetCacheFullPath fnCacheFullPath)
        {
            _fetcher = fetcher;
            _getCacheFullPath = fnCacheFullPath ?? throw new ArgumentNullException(nameof(fnCacheFullPath));
        }

        /// <summary>
        /// Requests contents using this object's fetcher, writing them to disk at the cache location.
        /// If the cache was unsuccessful, the retrieved contents are still returned.
        /// </summary>
        /// <returns>Stream of contents, null if there was an error or no contents were available.</returns>
        public Stream Get(string path)
        {
            try
            {
                var callbackFetcher = new CallbackResourceFetcher(_fetcher, (p, stream) =>
                {
                    // If there is an issue writing to the cache, we'll return the stream contents
                    // for use, they simply won't be cached.
                    // The stream might not support seeking, so we'll make a copy of it to work with,
                    // and to return back through the callback.
                    var streamCopy = new MemoryStream();
                    stream.CopyTo(streamCopy);
                    stream.Dispose();
                    streamCopy.Position = 0;

                    var cachePath = _getCacheFullPath(p);
                    WriteToFile(streamCopy, cachePath);

                    streamCopy.Position = 0;
                    return streamCopy;
                });

                return callbackFetcher.Get(path);
            }
            catch (Exception e)
            {
                Logger.Error($"Resource fetch failed for {path}", e);
                return null;
            }
        }

        private void WriteToFile(Stream stream, string cachePath)
        {
            try
            {
                var parentDirectory = Path.GetDirectoryName(cachePath);
                if (parentDirectory != null)
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                using (var outStream = File.OpenWrite(cachePath))
                {
                    stream.CopyTo(outStream);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error caching to {cachePath}", e);
            }
        }
    }
}
