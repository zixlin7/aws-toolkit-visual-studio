using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web;

using Amazon.AwsToolkit.Telemetry.Events.Generated;

using log4net;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    /// <summary>
    /// Retrieves data from a url
    /// </summary>
    public class HttpResourceFetcher : IResourceFetcher
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(HttpResourceFetcher));
        private readonly HttpResourceFetcherOptions _options;

        public HttpResourceFetcher(HttpResourceFetcherOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Requests contents from a specified url
        /// </summary>
        /// <returns>Stream of contents, null if there was an error or no contents were available.</returns>
        public virtual Stream Get(string url)
        {
            ToolkitGetExternalResource metric = new ToolkitGetExternalResource()
            {
                Url = url, Result = Result.Succeeded,
            };

            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    return null;
                }

                url = url.Replace(@"\", "/");

                var uri = new Uri(url);
                if (!uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                Logger.InfoFormat($"Loading resource: {url}");

                var webRequest = WebRequest.Create(uri);
                var response = webRequest.GetResponse();
                return response.GetResponseStream();
            }
            catch (Exception e)
            {
                metric.Result = Result.Failed;
                Logger.Error($"Failed to load resource: {url}", e);

                if (e is HttpException httpException)
                {
                    metric.Reason = httpException.ErrorCode.ToString(CultureInfo.InvariantCulture);
                }

                return null;
            }
            finally
            {
                _options.TelemetryLogger?.RecordToolkitGetExternalResource(metric);
            }
        }
    }
}
