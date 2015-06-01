using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AWSDeploymentHostManager.Persistence;

namespace AWSDeploymentHostManager
{
    public abstract class Version
    {
        public const string
            JSON_KEY_URL = "url",
            JSON_KEY_KEY = "s3key",
            JSON_KEY_BUCKET = "s3kucket",
            JSON_KEY_VERSION = "version",
            JSON_KEY_QUERY = "query_params",
            JSON_KEY_DEPLOYED = "deployed",
            JSON_KEY_ERROR = "error",
            JSON_KEY_CRYPT_KEY = "key",
            JSON_KEY_CRYPT_IV = "iv",
            JSON_KEY_DIGEST = "digest";

        protected EntityObject vers;

        public string Url
        {
            get
            {
                string url = null;
                vers.Parameters.TryGetValue(JSON_KEY_URL, out url);
                return url;
            }
            set
            {
                vers.Parameters[JSON_KEY_URL] = value;
            }
        }





        public string S3Key
        {
            get
            {
                string key = null;
                vers.Parameters.TryGetValue(JSON_KEY_KEY, out key);
                return key;
            }
            set
            {
                vers.Parameters[JSON_KEY_KEY] = value;
            }
        }

        public string S3Bucket
        {
            get
            {
                string bucket = null;
                vers.Parameters.TryGetValue(JSON_KEY_BUCKET, out bucket);
                return bucket;
            }
            set
            {
                vers.Parameters[JSON_KEY_BUCKET] = value;
            }
        }

        public string Digest
        {
            get
            {
                string digest = null;
                vers.Parameters.TryGetValue(JSON_KEY_DIGEST, out digest);
                return digest;
            }
            set
            {
                vers.Parameters[JSON_KEY_DIGEST] = value;
            }
        }

        public string S3Version
        {
            get
            {
                string version = null;
                vers.Parameters.TryGetValue(JSON_KEY_VERSION, out version);
                return version;
            }
            set
            {
                vers.Parameters[JSON_KEY_VERSION] = value;
            }
        }


        public string CryptoKey
        {
            get
            {
                string key = null;
                vers.Parameters.TryGetValue(JSON_KEY_CRYPT_KEY, out key);
                return key;
            }
            set
            {
                vers.Parameters[JSON_KEY_CRYPT_KEY] = value;
            }
        }

        public string CryptoIV
        {
            get
            {
                string iv = null;
                vers.Parameters.TryGetValue(JSON_KEY_CRYPT_IV, out iv);
                return iv;
            }
            set
            {
                vers.Parameters[JSON_KEY_CRYPT_IV] = value;
            }
        }

        public DateTime Timestamp
        {
            get  { return vers.Timestamp; }
        }

        protected void ParseUrl()
        {
            int startOfBucketName = -1;
            int endOfBucketName = -1;

            // Path style
            if (Url.Contains("://s3."))
            {
                startOfBucketName = Url.IndexOf(".com/") + 5;
                endOfBucketName = Url.IndexOf("/", startOfBucketName + 1);
            }
            else
            {
                startOfBucketName = Url.IndexOf("://") + 3;
                endOfBucketName = Url.IndexOf(".s3", startOfBucketName + 1);
            }

            S3Bucket = Url.Substring(startOfBucketName, endOfBucketName - startOfBucketName);

            int startOfKeyName = Url.IndexOf("/", endOfBucketName) + 1;
            int endOfKeyName = Url.IndexOf("?", startOfKeyName + 1);
            S3Key = Url.Substring(startOfKeyName, endOfKeyName - startOfKeyName);

            int startOfVersion = Url.IndexOf("versionId=") + "versionId=".Length;
            int endOfVersion = Url.IndexOf("&", startOfVersion);

            this.S3Version = Url.Substring(startOfVersion, endOfVersion - startOfVersion);
        }
    }
}
