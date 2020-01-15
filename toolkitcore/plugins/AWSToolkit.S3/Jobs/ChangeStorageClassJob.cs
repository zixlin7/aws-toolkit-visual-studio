using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.CommonUI.JobTracker;

using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Model;

using Amazon.S3;
using Amazon.S3.Util;

namespace Amazon.AWSToolkit.S3.Jobs
{
    public class ChangeStorageClassJob : QueueProcessingJob
    {
        BucketBrowserController _controller;
        List<BucketBrowserModel.ChildItem> _itemsToChanged;
        HashSet<string> _updatedFullPaths = new HashSet<string>();
        S3StorageClass _storageClass;

        public ChangeStorageClassJob(BucketBrowserController controller, List<BucketBrowserModel.ChildItem> itemsToChanged, S3StorageClass storageClass)
        {
            this._controller = controller;
            this._itemsToChanged = itemsToChanged;
            this._storageClass = storageClass;
            
            this.Title = string.Format("Change Storage Class");
        }

        protected override string CurrentStatusPostFix => "Change Storage Class";

        protected override Queue<QueueProcessingJob.IJobUnit> BuildQueueOfJobUnits()
        {
            this.CurrentStatus = "Generating list of keys";
            List<string> keysToChanged = this._controller.GetListOfKeys(this._itemsToChanged, true, StorageClass.NonGlacierS3StorageClasses.AsS3StorageClassSet());

            Queue<IJobUnit> units = new Queue<IJobUnit>();
            foreach (string key in keysToChanged)
            {
                units.Enqueue(new ChangeStorageClassUnit(this._controller, key, this._storageClass));
                _updatedFullPaths.Add(key);
            }

            this.CurrentStatus = string.Empty;
            return units;
        }

        protected override void PostExecuteJob(Exception exception)
        {
            if (exception != null)
                return;

            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    foreach (var item in _itemsToChanged)
                    {
                        if (item.ChildType == BucketBrowserModel.ChildType.File && _updatedFullPaths.Contains(item.FullPath))
                            item.StorageClass = this._storageClass;
                    }
                }));                    
        }

        public class ChangeStorageClassUnit : IJobUnit
        {
            BucketBrowserController _controller;
            string _key;
            S3StorageClass _storageClass;

            public ChangeStorageClassUnit(BucketBrowserController controller, string key, S3StorageClass storageClass)
            {
                this._controller = controller;
                this._key = key;
                this._storageClass = storageClass;
            }

            public void Execute()
            {
                AmazonS3Util.SetObjectStorageClass(this._controller.S3Client, this._controller.BucketName, this._key, this._storageClass);
            }
        }
    }
}
