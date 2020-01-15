using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI.JobTracker;

using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Model;

using Amazon.S3;
using Amazon.S3.Util;

namespace Amazon.AWSToolkit.S3.Jobs
{
    public class ChangeServerSideEncryptionJob : QueueProcessingJob
    {
        BucketBrowserController _controller;
        List<BucketBrowserModel.ChildItem> _itemsToChanged;
        ServerSideEncryptionMethod _method;

        public ChangeServerSideEncryptionJob(BucketBrowserController controller, List<BucketBrowserModel.ChildItem> itemsToChanged, ServerSideEncryptionMethod method)
        {
            this._controller = controller;
            this._itemsToChanged = itemsToChanged;
            this._method = method;
            
            this.Title = string.Format("Change Server Side Encryption");
        }

        protected override string CurrentStatusPostFix => "Change Server Side Encryption";

        protected override Queue<QueueProcessingJob.IJobUnit> BuildQueueOfJobUnits()
        {
            this.CurrentStatus = "Generating list of keys";
            List<string> keysToChanged = this._controller.GetListOfKeys(this._itemsToChanged, true, StorageClass.NonGlacierS3StorageClasses.AsS3StorageClassSet());

            Queue<IJobUnit> units = new Queue<IJobUnit>();
            foreach (string key in keysToChanged)
            {
                units.Enqueue(new ChangeServerSideEncryptionUnit(this._controller, key, this._method));
            }

            this.CurrentStatus = string.Empty;
            return units;
        }

        public class ChangeServerSideEncryptionUnit : IJobUnit
        {
            BucketBrowserController _controller;
            string _key;
            ServerSideEncryptionMethod _method;

            public ChangeServerSideEncryptionUnit(BucketBrowserController controller, string key, ServerSideEncryptionMethod method)
            {
                this._controller = controller;
                this._key = key;
                this._method = method;
            }

            public void Execute()
            {
                AmazonS3Util.SetServerSideEncryption(this._controller.S3Client, this._controller.BucketName, this._key, this._method);
            }
        }    
    }
}
