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
using Amazon.S3.Util;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

using log4net;


namespace Amazon.AWSToolkit.S3.Jobs
{
    public class RestoreObjectsJob : QueueProcessingJob
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(RestoreObjectsJob));

        BucketBrowserController _controller;
        List<BucketBrowserModel.ChildItem> _itemsToBeRestored;
        HashSet<BucketBrowserModel.ChildItem> _itemsSuccessfullyRemoved = new HashSet<BucketBrowserModel.ChildItem>();
        int _daysToKeepRestored;

        public RestoreObjectsJob(BucketBrowserController controller, List<BucketBrowserModel.ChildItem> itemsToBeRestored, int daysToKeepRestored)
        {
            this._controller = controller;
            this._itemsToBeRestored = itemsToBeRestored;
            this._daysToKeepRestored = daysToKeepRestored;

            this.Title = "Restore Files";
        }

        protected override string CurrentStatusPostFix
        {
            get { return "Restored files"; }
        }

        protected override Queue<QueueProcessingJob.IJobUnit> BuildQueueOfJobUnits()
        {
            this.CurrentStatus = "Generating list of keys";
            List<string> keysToBeRestored = this._controller.GetListOfKeys(this._itemsToBeRestored, false, S3Constants.LIST_OF_KEYS_GLACIER_STORAGE_CLASS);

            Queue<QueueProcessingJob.IJobUnit> units = new Queue<IJobUnit>();
            foreach (string key in keysToBeRestored)
            {
                units.Enqueue(new RestoreUnit(this, key, this._daysToKeepRestored));
            }

            this.CurrentStatus = string.Empty;
            this.Title = string.Format("Make {0} File(s) Restore", units.Count);
            return units;
        }

        public class RestoreUnit : IJobUnit
        {
            RestoreObjectsJob _job;
            string _key;
            int _days;

            public RestoreUnit(RestoreObjectsJob job, string key, int days)
            {
                this._job = job;
                this._key = key;
                this._days = days;
            }

            public void Execute()
            {
                var request = new RestoreObjectRequest()
                {
                    BucketName = this._job._controller.BucketName,
                    Key = this._key,
                    Days =_days
                };

                try
                {
                    this._job._controller.S3Client.RestoreObject(request);
                }
                catch (AmazonS3Exception e)
                {
                    LOGGER.Info(string.Format("Error restoring object {0} in bucket {1}", this._key, this._job._controller.BucketName), e);

                    if (this._job.TotalUnits == 1 || !string.Equals(e.ErrorCode, "RestoreAlreadyInProgress"))
                        throw;
                }
            }
        }
    }
}
