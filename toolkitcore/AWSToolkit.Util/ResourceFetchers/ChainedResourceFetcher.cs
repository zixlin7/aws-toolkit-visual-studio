using System;
using System.Collections.Generic;
using System.IO;
using log4net;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    /// <summary>
    /// Tries getting a resource from the provided fetchers, stopping when one returns a non-null stream.
    /// </summary>
    public class ChainedResourceFetcher : IResourceFetcher
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ChainedResourceFetcher));

        private readonly List<IResourceFetcher> _fetchers = new List<IResourceFetcher>();

        public ChainedResourceFetcher()
        {
        }

        public ChainedResourceFetcher(List<IResourceFetcher> fetchers)
        {
            _fetchers.AddRange(fetchers);
        }

        /// <summary>
        /// Adds a fetcher to the end of the chain of fetchers that would be retrieved from
        /// </summary>
        public ChainedResourceFetcher Add(IResourceFetcher fetcher)
        {
            if (fetcher != null)
            {
                _fetchers.Add(fetcher);
            }

            return this;
        }

        /// <summary>
        /// Requests contents using this object's chain of fetchers. Contents are returned from the first fetcher
        /// to successfully return non-null contents.
        /// </summary>
        /// <returns>Stream of contents, null if there was an error or no contents were available.</returns>
        public Stream Get(string path)
        {
            try
            {
                foreach (var resourceFetcher in _fetchers)
                {
                    if (TryGet(path, resourceFetcher, out var stream))
                    {
                        if (stream != null)
                        {
                            return stream;
                        }
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Resource fetch failed for {path}", e);
                return null;
            }
        }

        private static bool TryGet(string path, IResourceFetcher resourceFetcher, out Stream stream)
        {
            try
            {
                stream = resourceFetcher.Get(path);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Failure during Resource Fetcher chain", e);
                stream = null;
                return false;
            }
        }
    }
}
