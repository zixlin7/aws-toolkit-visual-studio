using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

using Amazon.SQS;
using Amazon.SQS.Model;

using Amazon.AWSToolkit.Navigator.Node;

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


        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.SQS.Resources.EmbeddedImages.queue-node.png";
            }
        }


        public string QueueUrl
        {
            get
            {
                return this._url;
            }
        }

        public string QueueARN
        {
            get
            {
                return this.SQSClient.GetQueueARN(this.SQSRootViewModel.CurrentEndPoint.RegionSystemName, this.QueueUrl);
            }
        }

        public SQSRootViewModel SQSRootViewModel
        {
            get
            {
                return this._serviceModel;
            }
        }

        public IAmazonSQS SQSClient
        {
            get
            {
                return this._sqsClient;
            }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.QueueUrl);
            dndDataObjects.SetData("ARN", this.QueueARN);
            dndDataObjects.SetData("URL", this.QueueUrl);
        }
    }
}
