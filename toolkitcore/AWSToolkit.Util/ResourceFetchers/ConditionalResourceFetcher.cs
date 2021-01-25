using System;
using System.IO;
using log4net;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    /// <summary>
    /// Retrieves data from a given resource fetcher, but only conditionally returns it.
    /// Callers can use this class to verify that the returned data is not corrupt for example.
    /// </summary>
    public class ConditionalResourceFetcher : IResourceFetcher
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ConditionalResourceFetcher));

        private readonly IResourceFetcher _fetcher;
        private readonly Func<Stream, bool> _shouldReturnStream;

        public ConditionalResourceFetcher(IResourceFetcher fetcher, Func<Stream, bool> fnShouldReturnStream)
        {
            _fetcher = fetcher;
            _shouldReturnStream = fnShouldReturnStream;
        }

        /// <summary>
        /// Requests contents using this object's fetcher. The contents are only returned if they pass
        /// this object's defined conditional, otherwise null is returned.
        /// </summary>
        /// <returns>Stream of contents, null if there was an error or no contents were available.</returns>
        public Stream Get(string path)
        {
            try
            {
                Stream returnStream = null;

                using (var stream = _fetcher.Get(path))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    returnStream = new MemoryStream();
                    stream.CopyTo(returnStream);
                    returnStream.Position = 0;
                }

                using (var testStream = new MemoryStream())
                {
                    returnStream.CopyTo(testStream);
                    testStream.Position = 0;

                    if (!_shouldReturnStream(testStream))
                    {
                        returnStream.Dispose();
                        return null;
                    }
                }

                returnStream.Position = 0;

                return returnStream;
            }
            catch (Exception e)
            {
                Logger.Error($"Resource fetch failed for {path}", e);
                return null;
            }
        }
    }
}
