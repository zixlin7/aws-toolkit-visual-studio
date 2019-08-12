using System.Collections.Generic;
using System.Windows;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.S3.Clipboard;
using log4net;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.S3.Nodes
{
    public class S3RootViewModel : ServiceRootViewModel, IS3ClipboardContainer, IS3RootViewModel
    {
        S3RootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;
        static ILog _logger = LogManager.GetLogger(typeof(S3RootViewModel));
        
        IAmazonS3 _s3Client;

        public S3RootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild<S3RootViewMetaNode>(), accountViewModel, "Amazon S3")
        {
            this._metaNode = base.MetaNode as S3RootViewMetaNode;
            this._accountViewModel = accountViewModel;            
        }

        public override string ToolTip => "Amazon S3 is storage for the Internet. It’s a simple storage service that offers software developers a highly-scalable, reliable, and low-latency data storage infrastructure at very low costs.";

        protected override string IconName => "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.service-root-icon.png";

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            var config = S3Utils.BuildS3Config(this.CurrentEndPoint);
            this._s3Client = new AmazonS3Client(awsCredentials, config);
        }


        public IAmazonS3 S3Client => this._s3Client;

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
                var child = new S3BucketViewModel(this._metaNode.S3BucketViewMetaNode, this, bucket.BucketName);
                items.Add(child);
            }

            BeginCopingChildren(items);
        }


        public void AddBucket(string bucketName)
        {
            S3BucketViewModel viewModel = new S3BucketViewModel(this._metaNode.S3BucketViewMetaNode, this, bucketName);
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
    }
}
