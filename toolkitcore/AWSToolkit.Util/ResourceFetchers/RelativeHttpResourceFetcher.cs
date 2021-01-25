using System;
using System.IO;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    /// <summary>
    /// Retrieves data from a url relative to a base location
    /// </summary>
    public class RelativeHttpResourceFetcher : HttpResourceFetcher
    {
        public class Options : HttpResourceFetcherOptions
        {
            public string BasePath { get; set; }
        }

        private readonly Options _options;

        private string BaseUrl => _options.BasePath + (_options.BasePath.EndsWith("/") ? "" : "/");

        public RelativeHttpResourceFetcher(Options options) : base(options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Requests contents from a specified url, relative to this object's base path.
        /// </summary>
        /// <returns>Stream of contents, null if there was an error or no contents were available.</returns>
        public override Stream Get(string relativeUrl)
        {
            var path = BaseUrl + relativeUrl;
            return base.Get(path);
        }
    }
}
