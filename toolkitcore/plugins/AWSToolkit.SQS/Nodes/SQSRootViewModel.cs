using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Amazon.SQS;
using Amazon.SQS.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.SQS.Nodes
{
    public class SQSRootViewModel : ServiceRootViewModel, ISQSRootViewModel
    {
        SQSRootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;
        IAmazonSQS _sqsClient;
        Dictionary<string, DateTime> _removedChildren = new Dictionary<string, DateTime>();

        public SQSRootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild<SQSRootViewMetaNode>(), accountViewModel, "Amazon SQS")
        {
            this._metaNode = base.MetaNode as SQSRootViewMetaNode;
            this._accountViewModel = accountViewModel;
        }

        public override string ToolTip => "Amazon Simple Queue Service (Amazon SQS) offers a reliable, highly scalable, hosted queue for storing messages as they travel between computers. By using Amazon SQS, developers can simply move data between distributed components of their applications that perform different tasks, without losing messages or requiring each component to be always available.";

        protected override string IconName => "Amazon.AWSToolkit.SQS.Resources.EmbeddedImages.service-root-icon.png";

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            var config = new AmazonSQSConfig();
            this.CurrentEndPoint.ApplyToClientConfig(config);
            this._sqsClient = new AmazonSQSClient(awsCredentials, config);
        }

        public IAmazonSQS SQSClient => this._sqsClient;

        protected override void LoadChildren()
        {
            try
            {
                var request = new ListQueuesRequest();
                ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
                var response = this.SQSClient.ListQueues(request);
                var items = response.QueueUrls.Select(url => new SQSQueueViewModel(this._metaNode.SQSViewMetaNode, this, url)).Where(child => !this._removedChildren.ContainsKey(child.Name)).Cast<IViewModel>().ToList();

                items.Sort(new Comparison<IViewModel>(AWSViewModel.CompareViewModel));
                BeginCopingChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }

        internal void RemovedQueue(SQSQueueViewModel child)
        {
            this._removedChildren[child.Name] = DateTime.Now;
            int index = 0;

            foreach(var instance in this.Children)
            {
                if (child.Name.Equals(instance.Name))
                    break;
                index++;
            }

            if (index < this.Children.Count)
                this.Children.RemoveAt(index);
        }

        internal void AddQueue(string queueUrl)
        {
            SQSQueueViewModel child = new SQSQueueViewModel(this._metaNode.SQSViewMetaNode, this, queueUrl);
            base.AddChild(child);
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData("ARN", string.Format("arn:aws:sqs:{0}:{1}:*",
                this.CurrentEndPoint.RegionSystemName, this.AccountViewModel.AccountNumber));
        }
    }
}
