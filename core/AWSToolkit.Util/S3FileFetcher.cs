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
    public class S3FileFetcher
    {
        public enum CacheMode { Never, PerInstance, Permanent, IfDifferent };

        const string CLOUDFRONT_CONFIG_FILES_LOCATION = @"https://d3rrggjwfhwld2.cloudfront.net/";
        const string S3_FALLBACK_LOCATION = @"https://aws-vs-toolkit.s3.amazonaws.com/";

        const string AWSVSTOOLKIT_BUCKETPREFIX = "aws-vs-toolkit";
        const string REGIONALENDPOINTSCHEME = "region://";

        ILog _logger = LogManager.GetLogger(typeof(S3FileFetcher));
        Dictionary<string, string> _filesFetched = new Dictionary<string, string>();

        static S3FileFetcher INSTANCE = new S3FileFetcher();
        private S3FileFetcher()
        {
        }

        public static S3FileFetcher Instance
        {
            get { return INSTANCE; }
        }

        // for testing purposes only
        Uri _testHostingFilesLocation;

        public Uri HostedFilesLocation
        {
            get
            {
                var configLocation = _testHostingFilesLocation != null ? 
                                _testHostingFilesLocation.OriginalString 
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

        public string GetFileContent(string filename)
        {
            return GetFileContent(filename, CacheMode.PerInstance);
        }

        public Stream OpenFileStream(string filename, CacheMode cacheMode)
        {
            try
            {
                filename = filename.Replace(@"\", "/");

                Stream stream = this.GetType().Assembly.GetManifestResourceStream("Amazon.AWSToolkit.HostedFiles." + filename.Replace('/', '.'));
                if (stream != null)
                {
                    _logger.InfoFormat("Loaded hosted file '{0}' from assembly resources", filename);
                    return stream; 
                }
                
                string localPath = null;
                bool cacheLocal = false;
                localPath = getLocalCachePath(filename);

                if (File.Exists(localPath) &&
                        (
                            cacheMode == CacheMode.Permanent ||
                            (cacheMode == CacheMode.IfDifferent && !this.IsLocalCacheDifferent(filename) && getLocalHostedFilesPath() == null) ||
                            (cacheMode == CacheMode.PerInstance && this._filesFetched.ContainsKey(filename))
                        )
                    )
                {
                    _logger.InfoFormat("Loading hosted file '{0}' from local path '{1}'", filename, localPath);
                    stream = File.OpenRead(localPath);
                    cacheLocal = false;
                }
                else
                {
                    string localHostedPath = getLocalHostedFilesPath();
                    if (localHostedPath != null)
                    {
                        string fullPath = Path.Combine(localHostedPath, filename);
                        if (File.Exists(fullPath))
                        {
                            _logger.InfoFormat("Loading hosted file '{0}' from path '{1}'", filename, fullPath);
                            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        }
                    }

                    try
                    {
                        try
                        {
                            string prefix = null;
                            try
                            {
                                var locationUri = Instance.HostedFilesLocation;
                                if (locationUri != null && 
                                    locationUri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                {
                                    prefix = locationUri.ToString();
                                    if (!prefix.EndsWith("/"))
                                        prefix = prefix + "/";

                                    _logger.InfoFormat("Probing for hosted file '{0}' at url prefix '{1}'", filename, prefix);

                                    HttpWebRequest httpRequest = WebRequest.Create(prefix + filename) as HttpWebRequest;
                                    HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;
                                    stream = response.GetResponseStream();
                                    cacheLocal = true;
                                }
                            }
                            catch (Exception e1)
                            {
                                _logger.Error("Failed to find config file " + filename + " under configured url prefix " + prefix, e1);
                            }

                            if (null == stream)
                            {
                                _logger.InfoFormat("Probing for hosted file '{0}' at url prefix '{1}'", filename, CLOUDFRONT_CONFIG_FILES_LOCATION);

                                HttpWebRequest httpRequest = WebRequest.Create(CLOUDFRONT_CONFIG_FILES_LOCATION + filename) as HttpWebRequest;
                                HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;
                                stream = response.GetResponseStream();
                                cacheLocal = true;
                            }
                        }
                        catch (Exception e2)
                        {
                            _logger.Error("Failed to find config file " + filename + " from cloudfront.", e2);

                            HttpWebRequest httpRequest = WebRequest.Create(S3_FALLBACK_LOCATION + filename) as HttpWebRequest;
                            HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;
                            stream = response.GetResponseStream();
                            cacheLocal = true;
                        }
                    }
                    catch
                    {
                        // If we failed to get the file from S3 then
                        // try and fallback to a local copy.
                        cacheLocal = false;
                        string cacheLocation = getLocalCachePath(filename);
                        if (File.Exists(cacheLocation))
                        {
                            _logger.InfoFormat("Probes for hosted file '{0}' failed, attempting local cache '{1}'", filename, cacheLocation);
                            stream = File.OpenRead(cacheLocation);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                if (CacheMode.Never != cacheMode && cacheLocal)
                {
                    byte[] cacheData;
                    if (cache(filename, stream, out cacheData))
                    {
                        // Now that the file is cached we can recall this method to have it 
                        // return the cached version.
                        return new MemoryStream(cacheData);
                    }
                    else
                    {
                        // Something bad happen trying to cache the data.  The state of the stream is unknown so
                        // we will attempt to get the object again with caching turned off.
                        return OpenFileStream(filename, CacheMode.Never);
                    }
                }


                return stream;
            }
            catch (Exception e)
            {
                this._logger.Error("Error getting " + filename + ".", e);
                return null;
            }
        }

        public string GetFileContent(string filename, CacheMode cacheMode)
        {
            try
            {
                filename = filename.Replace(@"\", "/");
                Stream stream = OpenFileStream(filename, cacheMode);

                using (StreamReader reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    return content;
                }
            }
            catch (Exception e)
            {
                this._logger.Error("Error getting " + filename + ".", e);
                return null;
            }
        }

        bool cache(string filename, Stream stream, out byte[] cachedData)
        {
            lock (INSTANCE)
            {
                try
                {
                    string path = getLocalCachePath(filename);
                    string rootDirectory = Path.GetDirectoryName(path);
                    Directory.CreateDirectory(rootDirectory);

                    using (FileStream outStream = File.Open(path, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[8192];
                        int read = 0;
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

        bool IsLocalCacheDifferent(string filename)
        {
            if (!string.IsNullOrEmpty(getLocalHostedFilesPath()))
                return false;

            var localPath = getLocalCachePath(filename);

            string localContent;
            using (StreamReader reader = new StreamReader(localPath))
                localContent = reader.ReadToEnd();

            var localMD5 = "\"" + Amazon.S3.Util.AmazonS3Util.GenerateChecksumForContent(localContent, false) + "\"";

            string remoteMD5 = null;
            try
            {
                HttpWebRequest httpRequest = WebRequest.Create(CLOUDFRONT_CONFIG_FILES_LOCATION + filename) as HttpWebRequest;
                httpRequest.Method = "HEAD";

                using (HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse)
                    remoteMD5 = response.Headers["ETag"];
            }
            catch (Exception e)
            {
                _logger.Info("Error checking etag to see if config file (" + filename + ") is different then local cache.", e);
                return false;
            }

            return !string.Equals(localMD5, remoteMD5, StringComparison.InvariantCultureIgnoreCase);
        }

        string getLocalCachePath(string filename)
        {
            string path = getLocalRepositoryDirectory() + filename;
            return path;
        }

        string getLocalRepositoryDirectory()
        {
            string folder = PersistenceManager.GetSettingsStoreFolder() + "/downloadedfiles";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder + "/";
        }

        static string getLocalHostedFilesPath()
        {
            string location = null;
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\AWSToolkit");
            if (key != null && key.GetValue("LocalHostedFilesPath") != null)
            {
                location = key.GetValue("LocalHostedFilesPath") as string;
                if (!Directory.Exists(location))
                    location = null;
            }

            var locationUri = Instance.HostedFilesLocation;
            if (locationUri != null && locationUri.IsFile)
                location = locationUri.LocalPath;

            if (location != null && !location.EndsWith("\\"))
                location += "\\";

            return location;
        }

        Uri ResolveRegionLocation(string location)
        {
            if (!location.StartsWith(REGIONALENDPOINTSCHEME, StringComparison.OrdinalIgnoreCase))
                return new Uri(location);

            var region = location.Substring(9);
            try
            {
                var endpoint = RegionEndpoint.GetBySystemName(region).GetEndpointForService("s3");
                return new Uri(string.Format("https://{0}-{1}.{2}/", AWSVSTOOLKIT_BUCKETPREFIX, region, endpoint));
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Failed to construct regional hosted files endpoint for location {0}", location);
            }

            return null;
        }

    }
}
