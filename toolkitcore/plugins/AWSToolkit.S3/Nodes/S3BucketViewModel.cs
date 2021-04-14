using System.Windows;
using Amazon.S3;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;

using log4net;

namespace Amazon.AWSToolkit.S3.Nodes
{
    public class S3BucketViewModel : AbstractViewModel, IS3BucketViewModel
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(S3BucketViewModel));

        private static readonly string BucketIcon = 
            "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.bucket.png";

        private static readonly string BucketPendingDeleteIcon =
            "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.bucket_pending_delete.png";

        object CLIENT_CREATE_LOCK = new object();

        S3BucketViewMetaNode _metaNode;
        S3RootViewModel _serviceModel;
        IAmazonS3 _rootS3Client;
        IAmazonS3 _localSpecificS3Client;
        string _bucketName;
        string _iconName;
        bool _isPendingDelete ;
        private IRegionProvider _regionProvider;

        public S3BucketViewModel(S3BucketViewMetaNode metaNode, S3RootViewModel viewModel, string bucketName, IRegionProvider regionProvider)
            : base(metaNode, viewModel, bucketName)
        {
            this._metaNode = metaNode;
            this._serviceModel = viewModel;
            this._rootS3Client = viewModel.S3Client;
            this._bucketName = bucketName;
            this._iconName = BucketIcon;
            this._regionProvider = regionProvider;
            // For consistency sake.
            _isPendingDelete=((S3RootViewMetaNode)_serviceModel.MetaNode).IsBucketBeingDeleted(this);            
        }

        protected override string IconName
        {
            get
            {
                if (((S3RootViewMetaNode)_serviceModel.MetaNode).IsBucketBeingDeleted(this))
                {
                    // Return a different icon if bucket is being deleted.
                    _iconName = BucketPendingDeleteIcon;
                }
                return _iconName;
            }
        }

        public bool PendingDeletion 
        {
            get => _isPendingDelete;
            internal set
            {
                _isPendingDelete = value;
                _iconName = _isPendingDelete ? BucketPendingDeleteIcon : BucketIcon;               
                NotifyPropertyChanged("Icon");
            }
        }

        public IAmazonS3 S3Client
        {
            get
            {
                if (this._localSpecificS3Client == null)
                {
                    buildLocalSpecificS3Client();
                }

                return this._localSpecificS3Client;
            }
        }

        private string _overrideRegion;
        /// <summary>
        /// Because the bucket can be in a different region then what the AWS Explorer is currently set to this
        /// property will contain the region the bucket is actually in.
        /// </summary>
        public string OverrideRegion
        {
            get
            {
                if(this._localSpecificS3Client == null)
                {
                    buildLocalSpecificS3Client();
                }

                return this._overrideRegion ?? ToolkitFactory.Instance.Navigator.SelectedRegion.Id;;
            }
        }

        private void buildLocalSpecificS3Client()
        {
            lock (CLIENT_CREATE_LOCK)
            {
                if (this._localSpecificS3Client != null)
                    return;

                S3Utils.BuildS3ClientForBucket(this.AccountViewModel, this._rootS3Client, this.Name, this._regionProvider, out this._localSpecificS3Client,  ref this._overrideRegion);
            }
        }

        public S3RootViewModel S3RootViewModel => this._serviceModel;

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.Name);
            dndDataObjects.SetData("ARN", string.Format("arn:aws:s3:::{0}", this.Name));
        }
    }
}
