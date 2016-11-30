using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI.JobTracker;

using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Model;

using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using Amazon.S3.Transfer;


namespace Amazon.AWSToolkit.S3.Jobs
{
    public class DownloadFilesJob : BaseJob
    {
        List<string> _keysToBeDownloaded;
        BucketBrowserController _controller;
        string _bucket;
        BucketBrowserModel.ChildItem[] _childItems;
        string _downloadDirectory;
        string _relativePath;


        long _currentFileProgressMin;
        long _currentFileProgressMax;
        long _currentFileProgressValue;

        string _currentFile;


        public DownloadFilesJob(BucketBrowserController controller, string bucketName, string relativePath, BucketBrowserModel.ChildItem[] childItems, string downloadDirectory)
        {

            this._controller = controller;
            this._bucket = bucketName;
            this._relativePath = relativePath;
            this._childItems = childItems;
            this._downloadDirectory = downloadDirectory.Replace(@"\", "/");

            if (!this._downloadDirectory.EndsWith("/"))
                this._downloadDirectory += "/";
            if(this._relativePath.StartsWith("/"))
                this._relativePath = this._relativePath.Substring(1);

            this.Title = "Download Files";
        }

        public virtual long CurrentFileProgressMin
        {
            get
            {
                return this._currentFileProgressMin;
            }
            set
            {
                this._currentFileProgressMin = value;
                base.NotifyPropertyChanged("CurrentFileProgressMin");
            }
        }

        public virtual long CurrentFileProgressMax
        {
            get
            {
                return this._currentFileProgressMax;
            }
            set
            {
                this._currentFileProgressMax = value;
                base.NotifyPropertyChanged("CurrentFileProgressMax");
            }
        }

        public virtual long CurrentFileProgressValue
        {
            get
            {
                return this._currentFileProgressValue;
            }
            set
            {
                this._currentFileProgressValue = value;
                base.NotifyPropertyChanged("CurrentFileProgressValue");
            }
        }

        protected override void ExecuteJob()
        {
            this.CurrentStatus = "Generating list of keys to download";
            this._keysToBeDownloaded = this._controller.GetListOfKeys(this._childItems, false);

            this.ProgressMin = 0;
            if (this._keysToBeDownloaded.Count == 1)
                this.ProgressMax = 0;
            else
                this.ProgressMax = this._keysToBeDownloaded.Count;

			this.ProgressValue = 0;

            TransferUtilityConfig config = new TransferUtilityConfig();
            TransferUtility utility = new TransferUtility(this._controller.S3Client, config);
            foreach (string key in this._keysToBeDownloaded)
            {
                string filepath = downloadLocation(key);
                this._currentFile = new FileInfo(filepath).Name;
                TransferUtilityDownloadRequest request = new TransferUtilityDownloadRequest()
                {
                    BucketName = this._bucket,
                    FilePath = filepath,
                    Key = key
                };
                request.WriteObjectProgressEvent += this.writeFileProgressCallback;

                try
                {
                    utility.Download(request);
                }
                catch (AmazonS3Exception e)
                {
                    if (e.ErrorCode == "InvalidObjectState")
                        throw new UnrestoredObjectException(string.Format("Object {0} needs to be restored before downloading.", key));
                    else
                        throw;
                }
                this.ProgressValue++;
            }
        }

        private string downloadLocation(string key)
        {
            string filepath = this._downloadDirectory + key.Substring(this._relativePath.Length);
            return filepath;
        }

        private void writeFileProgressCallback(object sender, WriteObjectProgressArgs e)
        {
            if (this._keysToBeDownloaded.Count == 1)
            {
                if(this.ProgressMax == 0)
                    this.ProgressMax = e.TotalBytes;

                this.ProgressValue = e.TransferredBytes;
            }
		
            this.CurrentFileProgressMin = 0;
            this.CurrentFileProgressMax = e.TotalBytes;
            this.CurrentFileProgressValue = e.TransferredBytes;
            this.CurrentStatus = string.Format("{0} {1} / {2} Bytes", this._currentFile, e.TransferredBytes.ToString("#,0"), e.TotalBytes.ToString("#,0"));
        }
    }
}
