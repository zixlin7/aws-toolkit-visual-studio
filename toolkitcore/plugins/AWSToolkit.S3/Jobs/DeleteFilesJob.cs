using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI.JobTracker;

using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Model;
using Amazon.S3.Model;


namespace Amazon.AWSToolkit.S3.Jobs
{
    public class DeleteFilesJob : QueueProcessingJob
    {
        BucketBrowserController _controller;
        List<BucketBrowserModel.ChildItem> _itemsToBeRemoved;
        HashSet<BucketBrowserModel.ChildItem> _itemsSuccessfullyRemoved = new HashSet<BucketBrowserModel.ChildItem>();
        int _numberOfToDeleteKeys;
        int _numberOfDeletedKeys;
        IDictionary<string, BucketBrowserModel.ChildItem> _keysToSelectedChildItems;

        public DeleteFilesJob(BucketBrowserController controller, List<BucketBrowserModel.ChildItem> itemsToBeRemoved)
        {
            this._controller = controller;
            this._itemsToBeRemoved = itemsToBeRemoved;

            this.Title = "Delete Files";
        }

        protected override int NumberActiveThreads => 2;

        protected override string CurrentStatusPostFix => "Deleted files";

        protected override Queue<QueueProcessingJob.IJobUnit> BuildQueueOfJobUnits()
        {
            this.CurrentStatus = "Generating list of keys";
            Queue<QueueProcessingJob.IJobUnit> units = new Queue<IJobUnit>();

            this._numberOfDeletedKeys = 0;
            this._numberOfToDeleteKeys = 0;
            this._keysToSelectedChildItems = new Dictionary<string, BucketBrowserModel.ChildItem>();
            foreach (var childItem in _itemsToBeRemoved)
            {
                var keysToBeDeleted = this._controller.GetListOfKeys(new BucketBrowserModel.ChildItem[] { childItem }, true);
                foreach (var key in keysToBeDeleted)
                {
                    this._keysToSelectedChildItems.Add(key, childItem);
                }
            }
            this._numberOfToDeleteKeys = this._keysToSelectedChildItems.Count;

            var keysPartition = new List<string>();
            foreach (var key in this._keysToSelectedChildItems.Keys)
            {
                keysPartition.Add(key);

                if (keysPartition.Count == S3Constants.MULTIPLE_OBJECT_DELETE_LIMIT)
                {
                    var unit = new DeleteObjectsUnit(this, keysPartition);
                    units.Enqueue(unit);
                    keysPartition = new List<string>();
                }
            }

            if (keysPartition.Count != 0)
            {
                var unit = new DeleteObjectsUnit(this, keysPartition);
                units.Enqueue(unit);
            }

            this.Title = string.Format("Delete {0} File(s)", this._numberOfToDeleteKeys);
            this.CurrentStatus = string.Empty;

            return units;
        }


        protected override int TotalUnits => this._numberOfToDeleteKeys;

        protected override int CompletedUnits => this._numberOfDeletedKeys;

        protected override void PostExecuteJob(Exception exception)
        {

            var hashStillFound = new HashSet<BucketBrowserModel.ChildItem>();
            foreach (var item in this._keysToSelectedChildItems.Values)
            {
                if (item != null && !hashStillFound.Contains(item))
                    hashStillFound.Add(item);
            }

            var hashItemsRemoved = new HashSet<BucketBrowserModel.ChildItem>();
            foreach (var item in this._itemsToBeRemoved)
            {
                if(!hashStillFound.Contains(item))
                    hashItemsRemoved.Add(item);
            }

            this._controller.RemoveChildItemsFromModel(hashItemsRemoved);
        }

        public class DeleteObjectsUnit : IJobUnit
        {
            DeleteFilesJob _job;
            IList<string> _s3Keys;

            public DeleteObjectsUnit(DeleteFilesJob job, IList<string> s3Keys)
            {
                this._job = job;
                this._s3Keys = s3Keys;
            }

            public void Execute()
            {
                var request = new DeleteObjectsRequest()
                {
                    BucketName = this._job._controller.BucketName,
                    Quiet = false
                };

                foreach (var key in this._s3Keys)
                {
                    request.AddKey(key);
                }

                var response = this._job._controller.S3Client.DeleteObjects(request);

                lock (this._job)
                {
                    this._job._numberOfDeletedKeys += response.DeletedObjects.Count;

                    foreach (var deletedObjects in response.DeletedObjects)
                    {
                        this._job._keysToSelectedChildItems[deletedObjects.Key] = null;
                    }
                }
            }
        }
    }
}
