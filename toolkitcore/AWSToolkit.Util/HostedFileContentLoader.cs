using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using log4net;

namespace Amazon.AWSToolkit
{
    public enum HostedFileContentLoadResult
    {
        Success,
        ResourceFallback,
        Failed
    }


    public interface IHostedFileContentLoader
    {
        /// <summary>
        /// Uses a default S3FileFetcher instance to obtain the hosted file content and returns the 
        /// content, or fallback content (if available) from resources.
        /// </summary>
        /// <param name="hostedFilename">The name of the hosted file to download/load from cache or local folder</param>
        /// <param name="cacheMode">The caching mode we want for the file</param>
        /// <param name="document">Output document mapped to the downloaded content, or built-in resource content.</param>
        /// <returns>
        /// Indicates if the supplied content loaded successfully or if we had to fallback
        /// to built-in resources, or failed totally.
        /// </returns>
        HostedFileContentLoadResult LoadXmlContent(string hostedFilename, S3FileFetcher.CacheMode cacheMode, out XDocument document);

        /// <summary>
        /// Attempts to load xml from the supplied content, falling back to the named hosted file
        /// in resources if an error occurs.
        /// </summary>
        /// <param name="reader">
        /// TextReader wrapping the stream containing the downloaded content, or content loaded
        /// from local cache.
        /// </param>
        /// <param name="hostedFilename">
        /// The name of the hosted file the content was read from.
        /// </param>
        /// <param name="fileFetcher">
        /// Optional file fetcher instance, used if we need to fall back to resources. If
        /// not supplied, a default S3FileFetcher instance will be instantiated and used.
        /// </param>
        /// <param name="document">
        /// Loaded XML document corresponding to the read content, or content from resources.
        /// </param>
        /// <returns>
        /// Indicates if the supplied content loaded successfully or if we had to fallback
        /// to built-in resources.
        /// </returns>
        HostedFileContentLoadResult LoadXmlContent(TextReader reader, string hostedFilename, S3FileFetcher fileFetcher, out XDocument document);
    }

    /// <summary>
    /// Wraps loading of hosted files containing xml and json content, falling back to
    /// build-in resource version of the file if an error occurs.
    /// </summary>
    public class HostedFileContentLoader : IHostedFileContentLoader
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(HostedFileContentLoader));
        private static readonly HostedFileContentLoader _instance = new HostedFileContentLoader();

        public static IHostedFileContentLoader Instance { get { return _instance; } }

        public HostedFileContentLoadResult LoadXmlContent(string hostedFilename, S3FileFetcher.CacheMode cacheMode, out XDocument document)
        {
            document = null;
            try
            {
                var reader = new StringReader(S3FileFetcher.Instance.GetFileContent(hostedFilename, S3FileFetcher.CacheMode.PerInstance));
                return LoadXmlContent(reader, hostedFilename, S3FileFetcher.Instance, out document);
            }
            catch (Exception e)
            {
                var logMsg = $"Failed to load {hostedFilename} from download/cache location or resource fallback";
                LOGGER.Error(logMsg, e);
            }

            return HostedFileContentLoadResult.Failed;
        }

        public HostedFileContentLoadResult LoadXmlContent(TextReader reader, string hostedFilename, S3FileFetcher fileFetcher, out XDocument document)
        {
            document = null;

            try
            {
                document = XDocument.Load(reader);
                return HostedFileContentLoadResult.Success;
            }
            catch (Exception e)
            {
                var logMsg = $"Failed to load {hostedFilename} from download/cache location, falling back to built-in resource";
                LOGGER.Error(logMsg, e);
            }

            var fetcher = fileFetcher ?? new S3FileFetcher();
            try
            {
                reader = new StreamReader(fetcher.GetFileContentFromResources(hostedFilename));
                document = XDocument.Load(reader);
                return HostedFileContentLoadResult.ResourceFallback;
            }
            catch (Exception e)
            {
                var logMsg = $"Failed to fallback to resources for {hostedFilename}";
                LOGGER.Error(logMsg, e);
            }

            return HostedFileContentLoadResult.Failed;
        }
    }
}
