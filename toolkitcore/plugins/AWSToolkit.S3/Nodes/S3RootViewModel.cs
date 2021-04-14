using System;
using System.Collections.Generic;
using System.Windows;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.S3.Clipboard;

namespace Amazon.AWSToolkit.S3.Nodes
{
    public class S3RootViewModel : ServiceRootViewModel, IS3ClipboardContainer, IS3RootViewModel
    {
        private readonly S3RootViewMetaNode _metaNode;
        private readonly IRegionProvider _regionProvider;
        private readonly Lazy<IAmazonS3> _s3Client;

        public S3RootViewModel(AccountViewModel accountViewModel, ToolkitRegion region, IRegionProvider regionProvider)
            : base(accountViewModel.MetaNode.FindChild<S3RootViewMetaNode>(), accountViewModel, "Amazon S3", region)
        {
            _metaNode = base.MetaNode as S3RootViewMetaNode;
            _regionProvider = regionProvider;
            _s3Client = new Lazy<IAmazonS3>(CreateS3Client);
        }

        public override string ToolTip => "Amazon S3 is storage for the Internet. It’s a simple storage service that offers software developers a highly-scalable, reliable, and low-latency data storage infrastructure at very low costs.";

        protected override string IconName => "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.service-root-icon.png";

        public IAmazonS3 S3Client => this._s3Client.Value;

        public S3Clipboard Clipboard
        {
            get;
            set;
        }


        protected override void LoadChildren()
        {
            List<IViewModel> items = new List<IViewModel>();
            var request = new ListBucketsRequest();

            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
            var response = this.S3Client.ListBuckets(request);
            foreach (var bucket in response.Buckets)
            {
                var child = new S3BucketViewModel(this._metaNode.S3BucketViewMetaNode, this, bucket.BucketName, _regionProvider);
                items.Add(child);
            }

            SetChildren(items);
        }


        public void AddBucket(string bucketName)
        {
            S3BucketViewModel viewModel = new S3BucketViewModel(this._metaNode.S3BucketViewMetaNode, this, bucketName, _regionProvider);
            base.AddChild(viewModel);
        }

        public void RemoveBucket(string bucketName)
        {
            base.RemoveChild(bucketName);
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData("ARN", "arn:aws:s3:::*");
        }

        public IAmazonS3 CreateS3Client()
        {
            return AccountViewModel.CreateServiceClient<AmazonS3Client>(Region);
        }
    }
}
