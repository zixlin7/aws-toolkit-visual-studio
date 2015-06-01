using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Security.Cryptography;

using log4net;

using Amazon.S3;
using Amazon.S3.Model;
using ThirdParty.Json.LitJson;
using Amazon.Util;

namespace AWSDeploymentHostManager
{
    public static class S3Util
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(S3Util));
        const int BufferSize = 8192;
        static AmazonS3Client s3Client;

        const string AWS_ACCESS_KEY_PATH = "credentials/AWS_ACCESS_KEY_ID";
        const string AWS_SECRET_KEY_PATH = "credentials/AWS_SECRET_KEY";

        const int MAX_DOWNLOAD_RETRY_ATTEMPTS = 6;

        public static HttpStatusCode UploadFile(string s3Url, string fileLocation, string contentType)
        {
            var request = WebRequest.Create(forceCanonicalPathAndQuery(s3Url)) as HttpWebRequest;
            request.ContentType = contentType;
            request.Method = "PUT";
            request.KeepAlive = false;
            request.AllowWriteStreamBuffering = false;
            request.AllowAutoRedirect = false;

            var fi = new FileInfo(fileLocation);
            request.ContentLength = fi.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                using (Stream inputStream = new FileStream(fileLocation, FileMode.Open))
                {
                    byte[] buffer = new byte[BufferSize];
                    int bytesRead = 0;
                    while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        requestStream.Write(buffer, 0, bytesRead);
                    }
                }
            }

            using (var response = request.GetResponse() as HttpWebResponse)
            {
                return response.StatusCode;
            }
        }

        public static void WriteToFile(string s3Url, string fileLocation)
        {
            int retries = 0;
            int position = 0;
            while (true)
            {
                try
                {
                    Stream fileStream;
                    if (position == 0)
                        fileStream = new FileStream(fileLocation, FileMode.Create);
                    else
                        fileStream = new FileStream(fileLocation, FileMode.Append);
                    using (fileStream)
                    {
                        using (var webStream = OpenStream(s3Url, position))
                        {
                            BufferedStream bufferedStream = new BufferedStream(webStream);
                            int bytesRead;
                            var buffer = new byte[10000];
                            while ((bytesRead = bufferedStream.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                            }
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (File.Exists(fileLocation))
                    {
                        int currentPosition = (int)new FileInfo(fileLocation).Length;
                        if (currentPosition != position)
                        {
                            LOGGER.Warn("WriteToFile download was interruppted and being reattempted from last location: " + currentPosition, e);
                            position = currentPosition;
                            continue;
                        }
                    }

                    if (retries < MAX_DOWNLOAD_RETRY_ATTEMPTS)
                    {
                        retries++;
                        LOGGER.Warn(string.Format("WriteToFile failed attempting retry({0}). URL: {1}", retries, s3Url), e);

                        int delay = (int)Math.Pow(4, retries) * 100;
                        System.Threading.Thread.Sleep(delay);
                    }
                    else
                    {
                        LOGGER.Error("WriteToFile failed for the last time and is aborting", e);
                        throw;
                    }
                }
            }
        }

        public static void WriteToFile(string bucket, string key, string fileLocation)
        {
            LOGGER.InfoFormat("Writing {0}:{1} to {2}", bucket, key, fileLocation);
            int retries = 0;
            while(true)
            {
                try
                {
                    using (var fileStream = File.OpenWrite(fileLocation))
                    {
                        using (var response = S3Client.GetObject(new GetObjectRequest()
                        {
                            BucketName = bucket,
                            Key = key
                        }))
                        {

                            using (var responseStream = response.ResponseStream)
                            {
                                BufferedStream bufferedStream = new BufferedStream(responseStream);
                                int bytesRead;
                                var buffer = new byte[10000];
                                while ((bytesRead = bufferedStream.Read(buffer, 0, buffer.Length)) != 0)
                                {
                                    fileStream.Write(buffer, 0, bytesRead);
                                }

                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (retries < MAX_DOWNLOAD_RETRY_ATTEMPTS)
                    {
                        retries++;
                        LOGGER.Warn(string.Format("WriteToFile failed attempting retry({0})", retries), e);

                        int delay = (int)Math.Pow(4, retries) * 100;
                        System.Threading.Thread.Sleep(delay);
                    }
                    else
                    {
                        LOGGER.Error("WriteToFile failed for the last time and is aborting", e);
                        throw;
                    }
                }
            }
        }

        public static string LoadContent(string s3Url)
        {
            DateTime dummy;
            return LoadContent(s3Url, out dummy);
        }

        public static string LoadContent(string s3Url, out DateTime lastModified)
        {
            using (var webStream = OpenStream(s3Url, out lastModified))
            {
                StreamReader reader = new StreamReader(webStream);
                string content = reader.ReadToEnd();
                return content;
            }
        }

        public static DateTime GetContentLastUpdated(string s3Url)
        {
            return GetContentLastUpdated(s3Url, HttpVerb.GET);
        }

        public static DateTime GetContentLastUpdated(string s3Url, HttpVerb Method)
        {
            DateTime lastModified;
            var httpRequest = WebRequest.Create(forceCanonicalPathAndQuery(s3Url)) as HttpWebRequest;
            httpRequest.Method = Method.ToString();
            using (var response = httpRequest.GetResponse() as HttpWebResponse)
            {
                string lastModHdr = response.Headers["Last-Modified"];
                try
                {
                    lastModified = DateTime.SpecifyKind(DateTime.ParseExact(lastModHdr,
                        AWSSDKUtils.GMTDateFormat, System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc);
                }
                catch
                {
                    lastModified = DateTime.UtcNow;
                }

                return lastModified;
            }
        }

        public static DateTime GetContentLastUpdated(string bucket, string key)
        {
            var response = S3Client.GetObjectMetadata(new GetObjectMetadataRequest()
            {
                BucketName = bucket,
                Key = key
            });
            
            return response.LastModified;
            
        }

        public static string LoadContent(string bucket, string key)
        {
            using (var response = S3Client.GetObject(new GetObjectRequest()
            {
                BucketName = bucket,
                Key = key
            }))
            {
                using (var clientStream = response.ResponseStream)
                {
                    StreamReader reader = new StreamReader(clientStream);
                    string content = reader.ReadToEnd();
                    return content;
                }
            }
        }

        public static string LoadContentWithUserData(string userdata, out DateTime lastModified)
        {
            LOGGER.InfoFormat("Userdata: {0}", userdata);
            JsonData json = JsonMapper.ToObject(userdata);
            IAmazonS3 s3Client = new AmazonS3Client((string)json["credentials"]["accessKey"], (string)json["credentials"]["secretKey"]);

            using (var response = s3Client.GetObject(new GetObjectRequest()
            {
                BucketName = (string)json["configuration"]["s3bucket"],
                Key = (string)json["configuration"]["s3key"]
            }))
            {

                string lastModHdr = response.Headers["Last-Modified"];

                try
                {
                    lastModified = DateTime.SpecifyKind(DateTime.ParseExact(lastModHdr,
                        AWSSDKUtils.GMTDateFormat, System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc);
                }
                catch
                {
                    lastModified = DateTime.UtcNow;
                }

                using (var clientStream = response.ResponseStream)
                {
                    StreamReader reader = new StreamReader(clientStream);
                    string content = reader.ReadToEnd();
                    return content;
                }
            }
        }

        private static IAmazonS3 S3Client
        {
            get
            {
                if (null == s3Client)
                {
                    string accessKey = HostManager.Config[AWS_ACCESS_KEY_PATH];
                    string secretKey = HostManager.Config[AWS_SECRET_KEY_PATH];
                    s3Client = new AmazonS3Client(accessKey, secretKey);
                }

                return s3Client;
            }
        }

        public static Stream OpenStream(string s3Url)
        {
            return OpenStream(s3Url, 0);
        }

        public static Stream OpenStream(string s3Url, int position)
        {
            var httpRequest = WebRequest.Create(forceCanonicalPathAndQuery(s3Url)) as HttpWebRequest;
            httpRequest.KeepAlive = false;
            httpRequest.AddRange(position);

            var response = httpRequest.GetResponse() as HttpWebResponse;
            return response.GetResponseStream();
        }
        public static Stream OpenStream(string s3Url, out DateTime lastModified)
        {
            var httpRequest = WebRequest.Create(forceCanonicalPathAndQuery(s3Url)) as HttpWebRequest;
            var response = httpRequest.GetResponse() as HttpWebResponse;
            
            string lastModHdr = response.Headers["Last-Modified"];
            try
            {
                lastModified = DateTime.SpecifyKind(DateTime.ParseExact(lastModHdr,
                    AWSSDKUtils.GMTDateFormat, System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc);
            }
            catch
            {
                lastModified = DateTime.UtcNow;
            }
            
            return response.GetResponseStream();
        }

        public static bool VerifyFileDigest(string fileName, string digest)
        {
            LOGGER.InfoFormat("Verifying file digest, if received.");

            if (string.IsNullOrEmpty(digest))
            {
                LOGGER.InfoFormat("Received digest is empty. skipping verification.");
                return true;
            }

            LOGGER.InfoFormat("Received digest: {0}", digest);
            string checksum = null;
            using (var fileStream = File.OpenRead(fileName))
            {
                checksum = GenerateChecksumForStream(fileStream);
                LOGGER.InfoFormat("Calculated file digest: {0}", checksum != null ? checksum : "(calculation returned null)");
                if (checksum != null && string.Compare(checksum, digest) == 0)
                    return true;
            }

            LOGGER.Error("Digest mismatch failure.");
            return false;
        }

        static Uri forceCanonicalPathAndQuery(string stringUri)
        {
            try
            {
                Uri uri = new Uri(stringUri);
                string paq = uri.PathAndQuery; // need to access PathAndQuery
                FieldInfo flagsFieldInfo = typeof(Uri).GetField("m_Flags", BindingFlags.Instance | BindingFlags.NonPublic);
                ulong flags = (ulong)flagsFieldInfo.GetValue(uri);
                flags &= ~((ulong)0x30); // Flags.PathNotCanonical|Flags.QueryNotCanonical
                flagsFieldInfo.SetValue(uri, flags);

                return uri;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error using reflection hack to fix .NET encoding issue. " + stringUri, e);
                throw;
            }
        }

        /// <summary>
        /// Generates an md5Digest for the file-stream specified
        /// </summary>
        /// <param name="input">The Stream for which the MD5 Digest needs
        /// to be computed.</param>
        /// <returns>A string representation of the hash without base64 encoding
        /// </returns>
        static string GenerateChecksumForStream(Stream input)
        {
            string hash = null;
            using (BufferedStream bstream = new BufferedStream(input, 1024 * 1024))
            {
                // Use an MD5 instance to compute the has for the stream
                byte[] hashed = MD5.Create().ComputeHash(bstream);
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < hashed.Length; i++)
                {
                    sBuilder.Append(hashed[i].ToString("x2"));
                }
                hash = sBuilder.ToString();
            }

            return hash;
        }
    }
}
