using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Amazon;
using Amazon.Runtime;

using Amazon.CloudFront;
using Amazon.CloudFront.Model;

using Amazon.S3;
using Amazon.S3.Transfer;

using Microsoft.Build.Framework;
using Amazon.S3.Model;
using BuildCommon;

namespace BuildTasks
{
    public abstract class S3TaskBase : BuildTaskBase
    {
        public string Bucket { get; set; }
        public string CredentialSet { get; set; }
        protected RegionEndpoint Region { get; set; }

        private IAmazonS3 _s3ClientOverride;

        public S3TaskBase()
        {
            Region = RegionEndpoint.USEast1;
        }

        /// <summary>
        /// Intended for tests to provide a mock client
        /// </summary>
        /// <param name="s3Client"></param>
        protected void SetS3ClientOverride(IAmazonS3 s3Client)
        {
            _s3ClientOverride = s3Client;
        }

        public string RegionValue
        {
            get
            {
                return Region == null ? string.Empty : Region.SystemName;
            }
            set
            {
                Region = RegionEndpoint.GetBySystemName(value);
                this.Log.LogMessage("Region set to {0}", Region.SystemName);
            }
        }

        protected IAmazonS3 S3Client
        {
            get
            {
                if (_s3ClientOverride != null)
                {
                    return _s3ClientOverride;
                }

                var region = Region ?? RegionEndpoint.USEast1;
                AWSCredentials credentials;
                if (string.IsNullOrEmpty(CredentialSet))
                    credentials = UploadCredentials.DefaultAWSCredentials;
                else
                {
                    credentials = UploadCredentials.AWSCredentials(CredentialSet);
                    if (credentials == null)
                        throw new ArgumentException("Unable to find credentials with name " + CredentialSet);
                }

                return new AmazonS3Client(credentials, region);
            }
        }

        protected AmazonCloudFrontClient CFClient
        {
            get
            {
                var region = Region ?? RegionEndpoint.USEast1;
                AWSCredentials credentials;
                if (string.IsNullOrEmpty(CredentialSet))
                    credentials = UploadCredentials.DefaultAWSCredentials;
                else
                {
                    credentials = UploadCredentials.AWSCredentials(CredentialSet);
                    if (credentials == null)
                        throw new ArgumentException("Unable to find credentials with name " + CredentialSet);
                }

                return new AmazonCloudFrontClient(credentials, region);
            }

        }

        protected TransferUtility TransferUtility { get { return new TransferUtility(S3Client); } }
    }

    public class UploadFileToS3Task : S3TaskBase
    {
        #region Methods

        public override bool Execute()
        {
            if (string.IsNullOrEmpty(File))
                throw new ArgumentNullException("File");
            if (string.IsNullOrEmpty(Bucket))
                throw new ArgumentNullException("Bucket");

            string fileName;
            if (File.Contains("*"))
            {
                var path = Path.GetFileName(File);
                var directory = Path.GetDirectoryName(File);
                var files = Directory.GetFiles(directory, Path.GetFileName(File), SearchOption.TopDirectoryOnly);
                if (files.Length != 1)
                {
                    this.Log.LogError("Should find one match but found {0} matches for {1}", files.Length, Path.GetFileName(File));
                }
                File = files[0];
                fileName = Path.GetFileName(File);
            }
            else
            {
                fileName = Path.GetFileName(File);
            }

            if (string.IsNullOrEmpty(Key))
                Key = fileName;

            if (string.IsNullOrEmpty(CannedACL))
                CannedACL = S3CannedACL.Private;

            this.Log.LogMessage("CredentialSet: {0}", this.CredentialSet);
            this.Log.LogMessage("Filepath: " + File);
            this.Log.LogMessage("Key: " + Key);
            this.Log.LogMessage("Upload Bucket: " + this.Bucket);

            var tu = TransferUtility;
            tu.Upload(new TransferUtilityUploadRequest
            {
                BucketName = Bucket,
                Key = Key,
                FilePath = File,
                CannedACL = CannedACL
            });

            return true;
        }

        #endregion

        #region Properties

        public string File { get; set; }
        public string Key { get; set; }
        public string CannedACL { get; set; }

        #endregion
    }

    public class UploadReleasesToS3Task : S3TaskBase
    {
        #region Methods

        public override bool Execute()
        {
            if (string.IsNullOrEmpty(File))
                throw new ArgumentNullException("File");
            if (string.IsNullOrEmpty(Bucket))
                throw new ArgumentNullException("Bucket");

            string fileName;
            if (File.Contains("*"))
            {
                var path = Path.GetFileName(File);
                var directory = Path.GetDirectoryName(File);
                var files = Directory.GetFiles(directory, Path.GetFileName(File), SearchOption.TopDirectoryOnly);
                if(files.Length != 1)
                {
                    this.Log.LogError("Should find one match but found {0} matches for {1}", files.Length, Path.GetFileName(File));
                }
                File = files[0];
                fileName = Path.GetFileName(File);
            }
            else
            {
                fileName = Path.GetFileName(File);
            }

            string latestKey = fileName;
            if (VersionedUpload)
            {
                int underscorePosition = fileName.IndexOf('_');
                if (underscorePosition < 0)
                    throw new Exception("Unable to determine version of file");
                string extension = Path.GetExtension(fileName);
                string baseFile = fileName.Substring(0, underscorePosition) + extension;

                latestKey = baseFile;
            }

            if (!string.IsNullOrEmpty(this.KeyPrefix))
            {
                if (!this.KeyPrefix.EndsWith("/"))
                    this.KeyPrefix += "/";

                latestKey = this.KeyPrefix + latestKey;
            }
            else
            {
                latestKey = "latest/" + latestKey;
            }


            this.Log.LogMessage("CredentialSet: {0}", this.CredentialSet);
            this.Log.LogMessage("Filepath: " + File);
            this.Log.LogMessage("File: " + fileName);
            this.Log.LogMessage("Upload Bucket: " + this.Bucket);
            if (VersionedUpload)
            {
                this.Log.LogMessageFromText("Latest Key: " + latestKey, MessageImportance.Normal);
            }

            TransferUtility tu = new TransferUtility(S3Client);
            if (VersionedUpload)
            {
                if (!this.LatestOnly)
                {
                    var key = fileName;
                    if (!string.IsNullOrEmpty(this.DirectoryName))
                    {
                        key = this.DirectoryName + "/" + fileName;
                    }

                    tu.Upload(new TransferUtilityUploadRequest
                    {
                        BucketName = Bucket,
                        Key = key,
                        FilePath = File,
                        CannedACL = S3CannedACL.PublicRead
                    });
                }

                var tuRequest = new TransferUtilityUploadRequest
                {
                    BucketName = Bucket,
                    Key = latestKey,
                    FilePath = File,
                    CannedACL = S3CannedACL.PublicRead
                };
                tuRequest.Headers.ContentDisposition = "attachment; filename=" + fileName;
                tu.Upload(tuRequest);
            }
            else
            {
                tu.Upload(new TransferUtilityUploadRequest
                {
                    BucketName = Bucket,
                    Key = latestKey,
                    FilePath = File,
                    CannedACL = S3CannedACL.PublicRead
                });
            }

            return true;
        }

        #endregion

        #region Properties

        public string File { get; set; }
        public bool VersionedUpload { get; set; }
        public bool LatestOnly { get; set; }
        public string KeyPrefix { get; set; }
        public string DirectoryName { get; set; }

        #endregion
    }

    /// <summary>
    /// Uploads a single hosted file to S3. Upload is skipped if source and destination have matching MD5 signatures.
    ///
    /// Key Inputs:
    /// Bucket - S3 Bucket to upload to
    /// S3Key - S3 Key to upload to
    /// LocalFilename - full path of file to upload
    /// </summary>
    public class UploadHostedFileTask : S3TaskBase
    {
        [Output]
        public string KeysToInvalidate { get; private set; }

        public string S3Key { get; set; }
        public string LocalFilename { get; set; }

        public override bool Execute()
        {
            CheckWaitForDebugger();

            var ret = true;
            try
            {
                if (S3Key.StartsWith(Utilities.KeySeparator))
                {
                    S3Key = S3Key.Substring(Utilities.KeySeparator.Length);
                }

                S3Item s3Item = GetS3Item(Bucket, S3Key, S3Client);
                bool fileExists = File.Exists(LocalFilename);
                bool s3Exists = s3Item != null;

                if (!fileExists)
                {
                    string message = s3Exists
                        ? $"Cancelling upload. The file exists in S3 bucket [{Bucket}] but not on local system: {LocalFilename}"
                        : $"Cancelling upload. The file is not in S3 bucket [{Bucket}] or locally: {LocalFilename}";

                    throw new FileNotFoundException(message);
                }

                bool uploadToS3 = true;

                if (s3Exists)
                {
                    var s3Md5 = s3Item.MD5;
                    var fileMd5 = FileMD5Util.GenerateMD5Hash(LocalFilename);

                    if (string.Equals(s3Md5, fileMd5, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.LogMessage("No differences found between S3 and local: {0}", LocalFilename);
                        uploadToS3 = false;
                    }
                }

                var invalidationKeys = new List<string>();
                if (uploadToS3)
                {
                    UploadFile(invalidationKeys);
                }

                if (invalidationKeys.Count > 0)
                {
                    KeysToInvalidate = string.Join(",", invalidationKeys);
                }
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                ret = false;
            }

            return ret;
        }

        private void UploadFile(List<string> invalidationKeys)
        {
            var client = S3Client;

            if (!File.Exists(LocalFilename))
            {
                throw new ArgumentException($"LocalFilename {LocalFilename} does not exist");
            }

            Log.LogMessage("Pushing file [{0}] to key [{1}]", LocalFilename, S3Key);

            var request = new PutObjectRequest
            {
                BucketName = Bucket,
                Key = S3Key,
                FilePath = LocalFilename,
                CannedACL = S3CannedACL.PublicRead
            };
            client.PutObject(request);

            var invalidationKey = $"{Utilities.KeySeparator}{S3Key}";
            invalidationKeys.Add(invalidationKey);
        }

        /// <summary>
        /// If the requested file exists in S3, an S3Item representing that file is returned
        /// </summary>
        /// <param name="bucket">S3 bucket to look in</param>
        /// <param name="key">S3 key to look for</param>
        /// <returns>Requested file, null if not found</returns>
        private static S3Item GetS3Item(string bucket, string key, IAmazonS3 s3Client)
        {
            var request = new ListObjectsRequest
            {
                BucketName = bucket,
                Prefix = key
            };

            ListObjectsResponse response;
            do
            {
                response = s3Client.ListObjects(request);

                foreach (var obj in response.S3Objects)
                {
                    // Listing for '/a/b/c' will return '/a/b/c', '/a/b/car', '/a/b/coat', etc
                    if (obj.Key == key)
                    {
                        return new S3Item(bucket, obj, s3Client);
                    }
                }
                request.Marker = response.NextMarker;
            } while (response.IsTruncated);

            return null;
        }
    }

    public class UploadHostedFilesTask : CompareToS3Task
    {
        [Output]
        public string KeysToInvalidate { get; private set; }

        public override bool Execute()
        {
            CheckWaitForDebugger();

            var ret = true;
            try
            {
                var fileExceptions = ExceptionsToHash(FileExceptions);
                var keyExceptions = ExceptionsToHash(KeyExceptions);   // assumption here that S3Path <-> LocalPath
                var comp = Compare(fileExceptions, keyExceptions, false, true);

                if (comp.DifferencesExist)
                {
                    if (comp.S3Only.Count > 0)
                        throw new Exception(string.Format("Cancelling upload. The following are hosted files in bucket [{0}] that are not on local system: {1}",
                                                          Bucket,
                                                          string.Join(", ", comp.S3Only)));

                    Log.LogMessage("There are differences, pushing");

                    var invalidationKeys = new List<string>();

                    UploadFiles(comp.DifferingPaths, invalidationKeys);
                    UploadFiles(comp.LocalOnly, invalidationKeys);

                    if (invalidationKeys.Count > 0)
                    {
                        string invalidationKeysValue = string.Join(",", invalidationKeys);
                        KeysToInvalidate = invalidationKeysValue;
                    }
                }
                else
                    Log.LogMessage("No differences found between S3 and local. {0} files examined.", comp.S3Items.Count);
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                ret = false;
            }

            return ret;
        }

        private void UploadFiles(List<string> paths, List<string> invalidationKeys)
        {
            var client = S3Client;

            var localPathInfo = new DirectoryInfo(LocalPath);
            if (!localPathInfo.Exists)
                throw new ArgumentException(string.Format("LocalPath {0} does not exist", LocalPath));

            foreach (var path in paths)
            {
                var key = path.Replace(Utilities.PathSeparator, Utilities.KeySeparator);
                var fullPath = Path.Combine(localPathInfo.FullName, path);
                Log.LogMessage("Pushing file [{0}] to key [{1}]", fullPath, key);
                Log.LogMessage("Bucket [{0}], Key: [{1}]", Bucket, key);
                var request = new PutObjectRequest
                {
                    BucketName = Bucket,
                    Key = key,
                    FilePath = fullPath,
                    CannedACL = S3CannedACL.PublicRead
                };
                client.PutObject(request);

                key = "/" + key;
                invalidationKeys.Add(key);
            }
        }
    }

    public class CFInvalidateTask : S3TaskBase
    {
        #region Methods

        public override bool Execute()
        {
            CheckWaitForDebugger();

            if (string.IsNullOrEmpty(Distribution))
                throw new ArgumentNullException("Distribution");
            if (string.IsNullOrEmpty(Keys))
                throw new ArgumentNullException("Keys");

            this.Log.LogMessage("CredentialSet: {0}", this.CredentialSet);
            this.Log.LogMessage("Distribution: {0}", this.Distribution);
            this.Log.LogMessage("Invalidating Keys: {0}", this.Keys);

            this.Log.LogMessage("Region: {0}", this.Region);

            string[] keysArray = Keys.Split(new string[] { KeySeparator }, StringSplitOptions.None);

            var invalidationRequest = new CreateInvalidationRequest { DistributionId = Distribution };
            var invalidationBatch = new InvalidationBatch { CallerReference = Guid.NewGuid().ToString(), Paths = new Paths() };
            invalidationBatch.Paths.Items.AddRange(keysArray);
            invalidationBatch.Paths.Quantity = keysArray.Length;
            invalidationRequest.InvalidationBatch = invalidationBatch;

            this.Log.LogMessage("Invalidating items with keys [" + string.Join(", ", keysArray) + "]");
            var response = CFClient.CreateInvalidation(invalidationRequest);

            if (WaitForInvalidation)
            {
                bool isDone = false;
                do
                {
                    var invalidationResponse = CFClient.GetInvalidation(new GetInvalidationRequest
                    {
                        DistributionId = Distribution,
                        Id = response.Invalidation.Id
                    });
                    if (string.Equals(invalidationResponse.Invalidation.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        this.Log.LogMessage("Invalidation completed....");
                        isDone = true;
                    }
                    else
                    {
                        this.Log.LogMessage("Current status of invalidation: " + invalidationResponse.Invalidation.Status);
                        this.Log.LogMessage("Sleeping....");
                        Thread.Sleep(TimeSpan.FromSeconds(60));
                    }
                } while (!isDone);
            }

            return true;
        }

        #endregion

        public CFInvalidateTask()
        {
            KeySeparator = ",";
        }

        #region Properties

        public string Distribution { get; set; }
        public string Keys { get; set; }
        public string KeySeparator { get; set; }
        public bool WaitForInvalidation { get; set; }

        #endregion
    }

}
