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

namespace BuildTasks
{
    public class DownloadFromS3Task: S3TaskBase
    {
        #region Methods

        public override bool Execute()
        {
            CheckWaitForDebugger();

            if (string.IsNullOrEmpty(S3Path) && string.IsNullOrEmpty(S3ObjectKey))
                throw new ArgumentException("One of S3Path or S3ObjectKey must be specified");
            if (string.IsNullOrEmpty(LocalPath))
                throw new ArgumentNullException("LocalPath");

            if (!Directory.Exists(LocalPath))
                Directory.CreateDirectory(LocalPath);

            this.Log.LogMessage("CredentialSet: {0}", this.CredentialSet);
            this.Log.LogMessage("LocalPath: " + LocalPath);
            if (string.IsNullOrEmpty(S3Path))
                this.Log.LogMessage("S3Path: " + S3Path);
            else
                this.Log.LogMessage("S3ObjectKey: " + S3ObjectKey);
            this.Log.LogMessage("Download Bucket: " + this.Bucket);

            var tu = TransferUtility;
            if (string.IsNullOrEmpty(S3Path))
            {
                tu.Download(new TransferUtilityDownloadRequest
                {
                    BucketName = Bucket,
                    Key = S3ObjectKey,
                    FilePath = Path.Combine(LocalPath, S3ObjectKey)
                });                                
            }
            else
            {
                tu.DownloadDirectory(new TransferUtilityDownloadDirectoryRequest
                {
                    BucketName = Bucket,
                    LocalDirectory = Path.Combine(LocalPath, S3Path),
                    S3Directory = S3Path
                });
            }

            return true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The key prefix identifying one or more objects to be downloaded to
        /// the folder specified as LocalPath. Specify this or S3ObjectKey.
        /// </summary>
        /// <returns></returns>
        public string S3Path { get; set; }

        /// <summary>
        /// The key identifying a single object to download to the folder
        /// specified as LocalPath.  Specify this or S3ObjectKey.
        /// </summary>
        public string S3ObjectKey { get; set; }

        /// <summary>
        /// The folder in which to place the downloaded object(s)
        /// </summary>
        /// <returns></returns>
        public string LocalPath { get; set; }

        #endregion
    }
}
