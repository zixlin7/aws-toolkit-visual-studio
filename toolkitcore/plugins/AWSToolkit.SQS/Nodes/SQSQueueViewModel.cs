using System.Windows;
using Amazon.SQS;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.SQS.Nodes
{
    public class SQSQueueViewModel : AbstractViewModel, ISQSQueueViewModel
    {
        SQSQueueViewMetaNode _metaNode;
        SQSRootViewModel _serviceModel;
        IAmazonSQS _sqsClient;
        string _url;

        public SQSQueueViewModel(SQSQueueViewMetaNode metaNode, SQSRootViewModel viewModel, string url)
            : base(metaNode, viewModel, url.Substring(url.LastIndexOf('/') + 1))
        {
            this._metaNode = metaNode;
            this._serviceModel = viewModel;
            this._sqsClient = viewModel.SQSClient;
            this._url = url;
        }


        protected override string IconName => AwsImageResourcePath.SqsQueue.Path;


        public string QueueUrl => this._url;

        public string QueueARN => this.SQSClient.GetQueueARN(this.SQSRootViewModel.Region.Id, this.QueueUrl);

        public SQSRootViewModel SQSRootViewModel => this._serviceModel;

        public IAmazonSQS SQSClient => this._sqsClient;

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.QueueUrl);
            dndDataObjects.SetData("ARN", this.QueueARN);
            dndDataObjects.SetData("URL", this.QueueUrl);
        }
    }
}
