using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI.JobTracker;

using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Model;

using Amazon.S3;
using Amazon.S3.Model;

namespace Amazon.AWSToolkit.S3.Jobs
{
    public class MakePublicJob : QueueProcessingJob
    {
        BucketBrowserController _controller;
        List<BucketBrowserModel.ChildItem> _itemsToBeMadePublic;

        public MakePublicJob(BucketBrowserController controller, List<BucketBrowserModel.ChildItem> itemsToBeMadePublic)
        {
            this._controller = controller;
            this._itemsToBeMadePublic = itemsToBeMadePublic;

            this.Title = "Make files publicly readable";
        }

        protected override string CurrentStatusPostFix => "Made Publicly Readable";

        protected override Queue<QueueProcessingJob.IJobUnit> BuildQueueOfJobUnits()
        {
            this.CurrentStatus = "Generating list of keys";
            List<string> keysToBeMadePublic = this._controller.GetListOfKeys(this._itemsToBeMadePublic, true);

            Queue<QueueProcessingJob.IJobUnit> units = new Queue<IJobUnit>();
            foreach (string key in keysToBeMadePublic)
            {
                units.Enqueue(new MakePublicUnit(this._controller, key));
            }

            this.CurrentStatus = string.Empty;
            this.Title = string.Format("Make {0} File(s) Publicly Readable", units.Count);
            return units;
        }

        public class MakePublicUnit : IJobUnit
        {
            BucketBrowserController _controller;
            string _key;

            public MakePublicUnit(BucketBrowserController controller, string key)
            {
                this._controller = controller;
                this._key = key;
            }

            public void Execute()
            {
                this._controller.S3Client.PutACL(new PutACLRequest()
                {
                    BucketName = this._controller.BucketName,
                    Key = this._key,
                    CannedACL = S3CannedACL.PublicRead
                });
            }
        }

    }
}
