using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit;

namespace Amazon.AWSToolkit.S3.Nodes
{
    public class S3RootViewMetaNode : ServiceRootViewMetaNode, IS3RootViewMetaNode
    {
        private object _bucketsBeingDeletedLock = new object();

        private List<string> _bucketsBeingDeleted = new List<string>();

        public const string S3_ENDPOINT_LOOKUP = "S3";

        public S3BucketViewMetaNode S3BucketViewMetaNode
        {
            get { return this.FindChild<S3BucketViewMetaNode>(); }
        }

        public override string EndPointSystemName
        {
            get { return S3_ENDPOINT_LOOKUP; }
        }

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new S3RootViewModel(account);
        }

        public ActionHandlerWrapper.ActionHandler OnCreate
        {
            get;
            set;
        }

        public void OnCreateResponse(IViewModel focus, ActionResults results)
        {
            S3RootViewModel rootModel = focus as S3RootViewModel;
            rootModel.AddBucket(results.FocalName as string);
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(new ActionHandlerWrapper("Create Bucket...", OnCreate, new ActionHandlerWrapper.ActionResponseHandler(this.OnCreateResponse), false, 
                    this.GetType().Assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.new-bucket.png"));
            }
        }

        public override string MarketingWebSite
        {
            get
            {
                return "http://aws.amazon.com/s3/";
            }
        }

        /// <summary>
        /// Adds a bucket to the list of buckets being deleted.
        /// This is used to track the buckets on which a delete operation is in progress.
        /// </summary>
        /// <param name="bucketModel">The bucket model.</param>
        internal void AddBucketToDeleteList(S3BucketViewModel bucketModel)
        {
            lock (_bucketsBeingDeletedLock)
            {
                _bucketsBeingDeleted.Add(bucketModel.Name);
            }
        }

        /// <summary>
        /// Remove the bucket from the list of buckets being deleted.
        /// </summary>
        /// <param name="bucketModel">The bucket model.</param>
        internal void RemoveBucketFromDeleteList(S3BucketViewModel bucketModel)
        {
            lock (_bucketsBeingDeletedLock)
            {
                if (_bucketsBeingDeleted.Contains(bucketModel.Name))
                {
                    _bucketsBeingDeleted.Remove(bucketModel.Name);
                }
            }
        }

        /// <summary>
        /// Checks if the specified bucket is being deleted.
        /// </summary>
        /// <param name="bucketModel">The bucket model.</param>
        /// <returns>Boolean value that indicates whether the bucket is being deleted.</returns>
        internal bool IsBucketBeingDeleted(S3BucketViewModel bucketModel)
        {
            lock (_bucketsBeingDeletedLock)
            {
                return _bucketsBeingDeleted.Contains(bucketModel.Name);
            }
        }
    }
}
