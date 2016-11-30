using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI.JobTracker;

using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Controller;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;


namespace Amazon.AWSToolkit.S3.Jobs
{
    public class UploadMultipleFilesJob : BaseJob
    {
        NewUploadSettingsModel _uploadSettingsModel;
        BucketBrowserController _controller;
        IAmazonS3 _s3Client;
        string _bucket;
        string[] _filepaths;
        string _filenameCurrentFile;
        string _s3RootFolder;
        string _localRoot;
        Owner _bucketOwner;
        List<BucketBrowserModel.ChildItem> _newChildItems = new List<BucketBrowserModel.ChildItem>();
        HashSet<string> _completedFiles = new HashSet<string>();

        S3AccessControlList _accessList;
        NameValueCollection _nvcMetadata;
        NameValueCollection _nvcHeaders;
        List<Tag> _tags;

        int _lastEventValue = -1;
        DateTime _lastEventTimestamp = DateTime.Now;

        public UploadMultipleFilesJob(BucketBrowserController controller, NewUploadSettingsModel uploadSettingsModel, string[] filepaths, string localRoot, string s3RootFolder,
            S3AccessControlList accessList, NameValueCollection nvcMetadata, NameValueCollection nvcHeaders, List<Tag> tags)
        {
            this._controller = controller;
            this._uploadSettingsModel = uploadSettingsModel;
            this._s3Client = this._controller.S3Client;
            this._bucket = this._controller.BucketName;
            this._filepaths = filepaths;
            this._localRoot = localRoot == null ? "" : localRoot.Replace(@"\", @"/");
            this._s3RootFolder = s3RootFolder;

            this._accessList = accessList;
            this._nvcMetadata = nvcMetadata;
            this._nvcHeaders = nvcHeaders;
            this._tags = tags;
            setTitle();
        }

        public override bool CanResume
        {
            get
            {
                return this._completedFiles.Count != this._filepaths.Length;
            }
        }

        void setTitle()
        {
            string verb = this.IsComplete ? "Uploaded" : "Uploading";
            if (this._filepaths.Length == 1)
                Title = string.Format("{0} {1}", verb, new FileInfo(this._filepaths[0]).Name);
            else
                Title = string.Format("{0} {1} Files", verb, this._filepaths.Length);
        }

        protected override void ResumeJob()
        {
            this.ExecuteJob();
        }

        protected override void ExecuteJob()
        {
            try
            {
                TransferUtilityConfig config = new TransferUtilityConfig();
                TransferUtility utility = new TransferUtility(this._s3Client, config);
                int index = this._completedFiles.Count;
                foreach (string filepath in this._filepaths)
                {
                    if (this._completedFiles.Contains(filepath))
                        continue;

                    this._filenameCurrentFile = new FileInfo(filepath).Name;
                    this._lastEventValue = -1;
                    this._lastEventTimestamp = DateTime.Now;
                    string key = determineKey(filepath);

                    updateFileProgressBar(index);
                    var request = new TransferUtilityUploadRequest()
                    {
                        FilePath = filepath,
                        BucketName = this._bucket,
                        Key = key
                    };
                    request.UploadProgressEvent += this.uploadTransferredBytesProgressCallback;


                    foreach (var name in this._nvcMetadata.AllKeys)
                        request.Metadata[name] = this._nvcMetadata[name];
                    foreach (var name in this._nvcHeaders.AllKeys)
                        request.Headers[name] = this._nvcHeaders[name];

                    if (this._uploadSettingsModel.UseReduceStorage)
                        request.StorageClass = S3StorageClass.ReducedRedundancy;
                    if (this._uploadSettingsModel.UseServerSideEncryption)
                        request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;
                    if (this._uploadSettingsModel.MakePublic)
                        request.CannedACL = S3CannedACL.PublicRead;


                    utility.Upload(request);

                    if(this._tags != null && this._tags.Count > 0)
                    {
                        this._s3Client.PutObjectTagging(new PutObjectTaggingRequest
                        {
                            BucketName = this._bucket,
                            Key = key,
                            Tagging = new Tagging
                            {
                                TagSet = this._tags
                            }
                        });
                    }

                    long fileSize = new FileInfo(filepath).Length;
                    var childItem = new BucketBrowserModel.ChildItem(key, fileSize, DateTime.Now, request.StorageClass);
                    this._newChildItems.Add(childItem);

                    if (this._accessList != null && this._accessList.Grants.Count > 0)
                    {
                        this._accessList.Owner = getBucketOwner();
                        utility.S3Client.PutACL(new PutACLRequest()
                        {
                            AccessControlList = this._accessList,
                            BucketName = this._bucket,
                            Key = key
                        });
                    }

                    this._completedFiles.Add(filepath);
                    index++;
                }
                updateFileProgressBar(index);
                this.IsComplete = true;
                setTitle();
            }
            finally
            {
                this.IsComplete = true;
                this._controller.AddChildItemsToModel(this._newChildItems);
            }
        }

        private Owner getBucketOwner()
        {
            if (this._bucketOwner == null)
            {
                var response = this._s3Client.GetACL(new GetACLRequest() { BucketName = this._bucket });
                this._bucketOwner = response.AccessControlList.Owner;
            }

            return this._bucketOwner;
        }

        private string determineKey(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
                return null;

            filepath = filepath.Replace(@"\", "/");
            if (!filepath.StartsWith(this._localRoot))
                return null;

            string relativeKey = filepath.Substring(this._localRoot.Length);
            if (relativeKey.StartsWith("/"))
                relativeKey = relativeKey.Substring(1);
            return this._s3RootFolder + relativeKey;
        }

        private void updateFileProgressBar(int index)
        {
            if (this._filepaths.Length > 1)
            {
                this.ProgressMin = 0;
                this.ProgressMax = this._filepaths.Length;
                this.ProgressValue = index;
            }
        }

        private void uploadTransferredBytesProgressCallback(object sender, UploadProgressArgs e)
        {
            int min = 0;
            int max = 100;
            int value = (int)((double)e.TransferredBytes / (double)e.TotalBytes * 100.0);

            if (this._filepaths.Length == 1)
            {
                if(this.ProgressMin != min)
                    this.ProgressMin = min;
                if (this.ProgressMax != max)
                    this.ProgressMax = max;
                if (this.ProgressValue != value || string.IsNullOrEmpty(this.CurrentStatus) || this._lastEventTimestamp.Ticks < DateTime.Now.AddSeconds(-1).Ticks)
                {
                    this._lastEventTimestamp = DateTime.Now;
                    this.CurrentStatus = string.Format("{0} / {1} Bytes", e.TransferredBytes.ToString("#,0"), e.TotalBytes.ToString("#,0"));
                    this.ProgressValue = value;
                }
            }
            else
            {
                if (this._lastEventValue != value || this._lastEventTimestamp.Ticks < DateTime.Now.AddSeconds(-1).Ticks || string.IsNullOrEmpty(this.CurrentStatus))
                {
                    this._lastEventValue = value;
                    this._lastEventTimestamp = DateTime.Now;
                    this.CurrentStatus = string.Format("{0}: {1} / {2} Bytes", this._filenameCurrentFile, e.TransferredBytes.ToString("#,0"), e.TotalBytes.ToString("#,0"));
                }
            }
        }
    }
}
