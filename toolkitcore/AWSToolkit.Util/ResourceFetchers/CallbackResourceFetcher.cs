using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using log4net;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    /// <summary>
    /// After getting data from a fetcher, the caller is notified with a callback.
    /// Useful for performing post-processing between fetcher calls.
    /// </summary>
    public class CallbackResourceFetcher : IResourceFetcher
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(CallbackResourceFetcher));

        /// <summary>
        /// Function that post-processes the stream after getting it from a resource fetcher
        /// </summary>
        /// <param name="path">The path used in the Get call</param>
        /// <param name="stream">The resulting stream from Get, may be null.
        /// This delegate is responsible for disposing the Stream if necessary.</param>
        /// <returns>The Stream to pass along from this object's Get call</returns>
        public delegate Task<Stream> PostProcessStream(string path, Stream stream);

        private readonly IResourceFetcher _resourceFetcher;
        private readonly PostProcessStream _postProcessStream;
      
        public CallbackResourceFetcher(IResourceFetcher resourceFetcher, PostProcessStream postProcessStream)
        {
            _resourceFetcher = resourceFetcher;
            _postProcessStream = postProcessStream ?? throw new ArgumentNullException(nameof(postProcessStream));
        }

        /// <summary>
        /// Requests contents using this object's fetcher, invoking this object's callback afterwards.
        /// </summary>
        /// <returns>Stream of contents, null if there was an error or no contents were available.</returns>
        public async Task<Stream> GetAsync(string path, CancellationToken token = default)
        {
            try
            {
                var stream = await _resourceFetcher.GetAsync(path, token);
                return await _postProcessStream.Invoke(path, stream);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to get resource: {path}", e);
                return null;
            }
        }
    }
}
