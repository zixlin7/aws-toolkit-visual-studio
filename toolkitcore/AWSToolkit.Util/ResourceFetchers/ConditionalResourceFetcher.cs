using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly Func<Stream, Task<bool>> _shouldReturnStream;
        // default buffer size reference: https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.copyto?view=netframework-4.7.2
        private readonly int _defaultBufferSize = 81920;

        public ConditionalResourceFetcher(IResourceFetcher fetcher, Func<Stream, Task<bool>> fnShouldReturnStream)
        {
            _fetcher = fetcher;
            _shouldReturnStream = fnShouldReturnStream;
        }

        /// <summary>
        /// Requests contents using this object's fetcher. The contents are only returned if they pass
        /// this object's defined conditional, otherwise null is returned.
        /// </summary>
        /// <returns>Stream of contents, null if there was an error or no contents were available.</returns>
        public async Task<Stream> GetAsync(string path, CancellationToken token = default)
        {
            try
            {
                Stream returnStream = null;

                using (var stream = await _fetcher.GetAsync(path, token))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    returnStream = new MemoryStream();
                    await stream.CopyToAsync(returnStream, _defaultBufferSize, token);
                    returnStream.Position = 0;
                }

                using (var testStream = new MemoryStream())
                {
                    await returnStream.CopyToAsync(testStream, _defaultBufferSize, token);
                    testStream.Position = 0;

                    if (!await _shouldReturnStream(testStream))
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
