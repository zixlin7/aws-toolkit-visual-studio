using System;
using System.Collections.Generic;
using System.Windows;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.SNS.Nodes
{
    public class SNSRootViewModel : ServiceRootViewModel, ISNSRootViewModel
    {
        private readonly SNSRootViewMetaNode _metaNode;
        private readonly Lazy<IAmazonSimpleNotificationService> _snsClient;

        public SNSRootViewModel(AccountViewModel accountViewModel, ToolkitRegion region)
            : base(accountViewModel.MetaNode.FindChild <SNSRootViewMetaNode>(), accountViewModel, "Amazon SNS", region)
        {
            _metaNode = base.MetaNode as SNSRootViewMetaNode;
            _snsClient = new Lazy<IAmazonSimpleNotificationService>(CreateSnsClient);
        }

        public override string ToolTip => "Amazon Simple Notification Service (Amazon SNS) is a web service that makes it easy to set up, operate, and send notifications from the cloud. It provides developers with a highly scalable, flexible, and cost-effective capability to publish messages from an application and immediately deliver them to subscribers or other applications. It is designed to make web-scale computing easier for developers.";

        protected override string IconName => "Amazon.AWSToolkit.SNS.Resources.EmbeddedImages.service-root-icon.png";

        public IAmazonSimpleNotificationService SNSClient => this._snsClient.Value;

        public void AddTopic(string topicArn)
        {
            SNSTopicViewModel child = new SNSTopicViewModel(this._metaNode.SNSTopicViewMetaNode, this, topicArn);
            base.AddChild(child);
        }

        public void RemoveTopic(string name)
        {
            int index = 0;
            foreach (var child in this.Children)
            {
                if (child.Name.Equals(name))
                {
                    break;
                }

                index++;
            }

            if (index < this.Children.Count)
            {
                this.Children.RemoveAt(index);
            }
        }


        protected override void LoadChildren()
        {
            try
            {
                List<IViewModel> items = new List<IViewModel>();
                ListTopicsResponse response = new ListTopicsResponse();
                do
                {
                    var request = new ListTopicsRequest() { NextToken = response.NextToken };
                    ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
                    response = this.SNSClient.ListTopics(request);

                    foreach (Topic topic in response.Topics)
                    {
                        var child = new SNSTopicViewModel(this._metaNode.SNSTopicViewMetaNode, this, topic.TopicArn);
                        items.Add(child);
                    }
                } while (!string.IsNullOrEmpty(response.NextToken));

                SetChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData("ARN", string.Format("arn:aws:sns:{0}:{1}:*",
                this.Region.Id, ToolkitFactory.Instance.AwsConnectionManager.ActiveAccountId));
        }

        private IAmazonSimpleNotificationService CreateSnsClient()
        {
            return AccountViewModel.CreateServiceClient<AmazonSimpleNotificationServiceClient>(Region);
        }
    }
}
