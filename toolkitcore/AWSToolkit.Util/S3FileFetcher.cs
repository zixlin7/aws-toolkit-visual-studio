using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Microsoft.Win32;

using log4net;

using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit
{
    /// <summary>
    /// Interface to help with testing of the file fetcher and construction of mocks
    /// </summary>
    public interface IS3FileFetcherContentResolver
    {
        Uri HostedFilesLocation { get; set; }
        string GetLocalCachePath(string filename);
        string GetLocalRepositoryDirectory();
        string GetUserConfiguredLocalHostedFilesPath();
        Uri ResolveRegionLocation(string location);
        HttpWebRequest ConstructWebRequest(string url);
    }

    /// <summary>
    /// Default content resolver that returns the real locations and web requests to obtain
    /// hosted files.
    /// </summary>
    internal class DefaultS3FileFetcherContentResolver : IS3FileFetcherContentResolver
    {
        private readonly ILog _logger;

        public DefaultS3FileFetcherContentResolver(ILog logger)
        {
            _logger = logger;
        }

        // for testing purposes only
        Uri _testHostingFilesLocation;

        public Uri HostedFilesLocation
        {
            get
            {
                var configLocation = _testHostingFilesLocation != null 
                    ? _testHostingFilesLocation.OriginalString
                    : PersistenceManager.Instance.GetSetting(ToolkitSettingsConstants.HostedFilesLocation);

                if (!string.IsNullOrEmpty(configLocation))
                {
                    var resolvedLocation = ResolveRegionLocation(configLocation);
                    _logger.InfoFormat("Found hosted files config override '{0}', resolved to '{1}'",
                        configLocation,
                        resolvedLocation);
                    return resolvedLocation;
                }

                _logger.InfoFormat("Null/empty hosted files location override");
                return null;
            }

            set
            {
                // for testing purposes only
                _testHostingFilesLocation = value;
            }
        }

        public string GetLocalCachePath(string filename)
        {
            var path = GetLocalRepositoryDirectory() + filename;
            return path;
        }

        public string GetLocalRepositoryDirectory()
        {
            var folder = PersistenceManager.GetSettingsStoreFolder() + "/downloadedfiles";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder + "/";
        }

        public string GetUserConfiguredLocalHostedFilesPath()
        {
            const string SubKeyName = @"Software\AWSToolkit";
            const string KeyName = "LocalHostedFilesPath";

            string location = null;
            try
            {
                var key = Registry.LocalMachine.OpenSubKey(SubKeyName);
                if (key?.GetValue(KeyName) != null)
                {
                    location = key.GetValue(KeyName) as string;
                    if (!Directory.Exists(location))
                        location = null;
                }

                var locationUri = HostedFilesLocation;
                if (locationUri != null && locationUri.IsFile)
                    location = locationUri.LocalPath;

                if (location != null && !location.EndsWith("\\"))
                    location += "\\";
            }
            catch (Exception e)
            {
                _logger.Error("Error attempting to read value for " + KeyName + " from registry subkey " + SubKeyName, e);
                throw;
            }

            return location;
        }

        public Uri ResolveRegionLocation(string location)
        {
            if (!location.StartsWith(S3FileFetcher.REGIONALENDPOINTSCHEME, StringComparison.OrdinalIgnoreCase))
                return new Uri(location);

            var region = location.Substring(9);
            try
            {
                var endpoint = RegionEndpoint.GetBySystemName(region).GetEndpointForService("s3");
                return new Uri(string.Format("https://{0}-{1}.{2}/", S3FileFetcher.AWSVSTOOLKIT_BUCKETPREFIX, region, endpoint));
            }
            catch
            {
                _logger.ErrorFormat("Failed to construct regional hosted files endpoint for location {0}", location);
            }

            return null;
        }

        public HttpWebRequest ConstructWebRequest(string url)
        {
            return WebRequest.Create(url) as HttpWebRequest;
        }

    }

    public class S3FileFetcher
    {
        public enum CacheMode { Never, PerInstance, Permanent, IfDifferent };

        /// <summary>
        /// This is used for testing so we can verify the search pipeline resolves
        /// to what we expect given a test setup
        /// </summary>
        public enum ResolvedLocation
        {
            Failed,
            Cache,
            ConfiguredFolder,
            CloudFront,
            S3,
            Resources
        }

        public ResolvedLocation ResolvedContentLocation { get; private set; }

        public const string CLOUDFRONT_CONFIG_FILES_LOCATION = @"https://d3rrggjwfhwld2.cloudfront.net/";
        public const string S3_FALLBACK_LOCATION = @"https://aws-vs-toolkit.s3.amazonaws.com/";

        public const string AWSVSTOOLKIT_BUCKETPREFIX = "aws-vs-toolkit";
        public const string REGIONALENDPOINTSCHEME = "region://";

        private readonly ILog _logger = LogManager.GetLogger(typeof(S3FileFetcher));
        private readonly Dictionary<string, string> _filesFetched = new Dictionary<string, string>();

        private readonly IS3FileFetcherContentResolver _contentResolver;

        // ReSharper disable once InconsistentNaming
        private static readonly S3FileFetcher _Instance = new S3FileFetcher();

        public S3FileFetcher()
        {
            _contentResolver = new DefaultS3FileFetcherContentResolver(_logger);
        }

        public S3FileFetcher(IS3FileFetcherContentResolver testResolver)
        {
            _contentResolver = testResolver;
        }

        public static S3FileFetcher Instance
        {
            get { return _Instance; }
        }

        public string GetFileContent(string filename)
        {
            return GetFileContent(filename, CacheMode.PerInstance);
        }

        public string GetFileContent(string filename, CacheMode cacheMode)
        {
            try
            {
                filename = filename.Replace(@"\", "/");
                var stream = OpenFileStream(filename, cacheMode);

                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();
                    return content;
                }
            }
            catch (Exception e)
            {
                this._logger.Error("Error getting " + filename + ".", e);
                return null;
            }
        }

        /// <summary>
        /// Used to enable us to force-load from resource in the face of a bad
        /// download or other corrupted file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public Stream GetFileContentFromResources(string filename)
        {
            filename = filename.Replace(@"\", "/");
            return LoadFromToolkitResources(filename);
        }

        // virtual to allow mock in testing
        public virtual Stream OpenFileStream(string filename, CacheMode cacheMode)
        {
            filename = filename.Replace(@"\", "/");
            var canCacheLocal = false;

            var fileStream = LoadFromConfiguredHostedFilesFolder(filename)                      // registry or folder path set in options dialog
                             ?? LoadFromUserProfileCache(filename, cacheMode)                   // appdata cache folder
                             ?? LoadFromConfiguredHostedFilesUri(filename, out canCacheLocal)   // region: or uri configured location in options dialog
                             ?? LoadFromUrl(CLOUDFRONT_CONFIG_FILES_LOCATION + filename, out canCacheLocal) // preferred
                             ?? LoadFromUrl(S3_FALLBACK_LOCATION + filename, out canCacheLocal) // backup preference
                             ?? LoadFromUserProfileCache(filename); // everything failed but we have cached so use it anyway as last-but-one resort

            // if we got content from an online source, then consider caching it in the user profile
            if (fileStream != null && CacheMode.Never != cacheMode && canCacheLocal)
            {
                byte[] cacheData;
                if (CacheFileContent(filename, fileStream, out cacheData))
                {
                    // Now that the file is cached we can recall this method to have it 
                    // return the cached version.
                    fileStream = new MemoryStream(cacheData);
                }
                else
                {
                    // Something bad happen trying to cache the data.  The state of the stream is unknown so
                    // we will attempt to get the object again with caching turned off.
                    fileStream = OpenFileStream(filename, CacheMode.Never);
                }
            }

            // last gasp attempt to get content if we could not get it from configured, cached or online source
            // (we of course don't need to put this into the cache)
            return fileStream ?? LoadFromToolkitResources(filename);
        }

        /// <summary>
        /// Tests for content in user's appdata cache, downloaded from a previous run,
        /// if compatible with preferred cache setting
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="cacheMode"></param>
        /// <returns></returns>
        private Stream LoadFromUserProfileCache(string filename, CacheMode cacheMode)
        {
            var localPath = _contentResolver.GetLocalCachePath(filename);
            if (string.IsNullOrEmpty(localPath))
                return null;

            Stream fileStream = null;
            try
            {
                // note a configured local folder (getLocalHostedFilesPath) for hosted files should override any cache setting
                if (File.Exists(localPath) &&
                    (
                        cacheMode == CacheMode.Permanent
                        || cacheMode == CacheMode.IfDifferent &&
                        _contentResolver.GetUserConfiguredLocalHostedFilesPath() == null &&
                        !this.IsLocalCacheDifferent(filename)
                        || cacheMode == CacheMode.PerInstance && this._filesFetched.ContainsKey(filename)
                    )
                )
                {
                    _logger.InfoFormat("Loading hosted file '{0}' from local path '{1}'", filename, localPath);
                    fileStream = File.OpenRead(localPath);
                }
            }
            catch (Exception e)
            {
                var logMsg = string.Format("Failed to access hosted file {0} from userprofile cache location", filename);
                _logger.Error(logMsg, e);
            }
            finally
            {
                if (fileStream != null)
                {
                    ResolvedContentLocation = ResolvedLocation.Cache;
                }
            }

            return fileStream;
        }

        /// <summary>
        /// Last-gasp attempt, bar loading from resources - if the file exists in the cache by virtue of
        /// previous run, irrespective of cache mode, try and use it
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private Stream LoadFromUserProfileCache(string filename)
        {
            var localPath = _contentResolver.GetLocalCachePath(filename);
            if (string.IsNullOrEmpty(localPath))
                return null;

            Stream fileStream = null;
            try
            {
                if (File.Exists(localPath))
                {
                    _logger.InfoFormat("Loading hosted file '{0}' from local path '{1}'", filename, localPath);
                    fileStream = File.OpenRead(localPath);
                }
            }
            catch (Exception e)
            {
                var logMsg = string.Format("Failed to access hosted file {0} from userprofile cache location",
                    filename);
                _logger.Error(logMsg, e);
            }
            finally
            {
                if (fileStream != null)
                {
                    ResolvedContentLocation = ResolvedLocation.Cache;
                }
            }

            return fileStream;
        }

        /// <summary>
        /// Checks location specified by user in registry or toolkit's miscsettings file. Can be
        /// folder, regional (eg cn-north-1) or uri setting to the content.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private Stream LoadFromConfiguredHostedFilesFolder(string filename)
        {
            var localHostedPath = _contentResolver.GetUserConfiguredLocalHostedFilesPath();
            if (string.IsNullOrEmpty(localHostedPath))
                return null;

            Stream fileStream = null;
            try
            {
                var fullPath = Path.Combine(localHostedPath, filename);
                if (File.Exists(fullPath))
                {
                    _logger.InfoFormat("Loading hosted file '{0}' from path '{1}'", filename, fullPath);
                    fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
            }
            catch (Exception e)
            {
                var logMsg =
                    string.Format("Failed to access hosted file {0} from configured hosted files location {1}",
                        filename, localHostedPath);
                _logger.Error(logMsg, e);
            }
            finally
            {
                if (fileStream != null)
                {
                    ResolvedContentLocation = ResolvedLocation.ConfiguredFolder;
                }
            }

            return fileStream;
        }

        /// <summary>
        /// Handles custom locations configured by the user as a uri or region://
        /// designation.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="cacheLocal"></param>
        /// <returns></returns>
        private Stream LoadFromConfiguredHostedFilesUri(string filename, out bool cacheLocal)
        {
            var prefix = string.Empty;
            Stream fileStream = null;
            cacheLocal = false;

            try
            {
                var locationUri = _contentResolver.HostedFilesLocation;
                if (locationUri != null &&
                    locationUri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    prefix = locationUri.ToString();
                    if (!prefix.EndsWith("/"))
                        prefix = prefix + "/";

                    _logger.InfoFormat("Probing for hosted file '{0}' at url prefix '{1}'", filename, prefix);

                    var httpRequest = _contentResolver.ConstructWebRequest(prefix + filename);
                    var response = httpRequest.GetResponse() as HttpWebResponse;
                    fileStream = response.GetResponseStream();
                }
            }
            catch (Exception e)
            {
                var logMsg = string.Format("Failed to load hosted file {0} under configured url prefix {1}", filename, prefix);
                _logger.Error(logMsg, e);
            }
            finally
            {
                if (fileStream != null)
                {
                    cacheLocal = true;
                    ResolvedContentLocation = ResolvedLocation.ConfiguredFolder;
                }
            }

            return fileStream;
        }

        /// <summary>
        /// Used to probe for content in CloudFront or S3 locations.
        /// </summary>
        /// <param name="targetUrl"></param>
        /// <param name="cacheLocal"></param>
        /// <returns></returns>
        private Stream LoadFromUrl(string targetUrl, out bool cacheLocal)
        {
            Stream fileStream = null;
            cacheLocal = false;

            try
            {
                _logger.InfoFormat("Probing for hosted file at url '{0}'", targetUrl);

                var httpRequest = _contentResolver.ConstructWebRequest(targetUrl);
                var response = httpRequest.GetResponse() as HttpWebResponse;
                fileStream = response.GetResponseStream();
            }
            catch (Exception e)
            {
                var logMsg = string.Format("Failed to load hosted file {0}", targetUrl);
                _logger.Error(logMsg, e);
            }
            finally
            {
                if (fileStream != null)
                {
                    cacheLocal = true;
                    ResolvedContentLocation = ResolvedLocation.CloudFront;
                }
            }

            return fileStream;
        }

        /// <summary>
        /// Handles final attempt to get content by checking in application resources.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private Stream LoadFromToolkitResources(string filename)
        {
            Stream fileStream = null;
            try
            {
                fileStream = this.GetType().Assembly.GetManifestResourceStream("Amazon.AWSToolkit.HostedFiles." + filename.Replace('/', '.'));
                if (fileStream != null)
                {
                    _logger.InfoFormat("Loaded hosted file '{0}' from assembly resources", filename);
                    ResolvedContentLocation = ResolvedLocation.Resources;
                }
            }
            catch (Exception e)
            {
                var logMsg = string.Format("Failed to load hosted file {0} from toolkit resources", filename);
                _logger.Error(logMsg, e);
            }

            return fileStream;
        }

        private bool CacheFileContent(string filename, Stream stream, out byte[] cachedData)
        {
            lock (_Instance)
            {
                try
                {
                    var path = _contentResolver.GetLocalCachePath(filename);
                    var rootDirectory = Path.GetDirectoryName(path);
                    Directory.CreateDirectory(rootDirectory);

                    using (var outStream = File.Open(path, FileMode.Create, FileAccess.Write))
                    {
                        var buffer = new byte[8192];
                        var read = 0;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            outStream.Write(buffer, 0, read);
                        }
                    }

                    this._filesFetched[filename] = path;

                    cachedData = File.ReadAllBytes(path);
                    return true;
                }
                catch (Exception e)
                {
                    this._logger.Error("Error caching " + filename + ".", e);
                    cachedData = null;
                    return false;
                }
            }
        }

        private bool IsLocalCacheDifferent(string filename)
        {
            if (!string.IsNullOrEmpty(_contentResolver.GetUserConfiguredLocalHostedFilesPath()))
                return false;

            var localPath = _contentResolver.GetLocalCachePath(filename);

            string localContent;
            using (var reader = new StreamReader(localPath))
            {
                localContent = reader.ReadToEnd();
            }

            var localMD5 = "\"" + Amazon.S3.Util.AmazonS3Util.GenerateChecksumForContent(localContent, false) + "\"";

            string remoteMD5;
            try
            {
                var httpRequest = _contentResolver.ConstructWebRequest(CLOUDFRONT_CONFIG_FILES_LOCATION + filename);
                httpRequest.Method = "HEAD";

                using (var response = httpRequest.GetResponse() as HttpWebResponse)
                {
                    remoteMD5 = response.Headers["ETag"];
                }
                return !string.Equals(localMD5, remoteMD5, StringComparison.InvariantCultureIgnoreCase);
            }
            catch (Exception e)
            {
                _logger.Info("Error checking etag to see if config file (" + filename + ") is different then local cache.", e);
                return false; // return false so we simply use the cache version as if it were up to date
            }
        }

    }

}
