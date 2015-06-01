using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SNS.Nodes
{
    public class SNSRootViewModel : ServiceRootViewModel, ISNSRootViewModel
    {
        SNSRootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;
        IAmazonSimpleNotificationService _snsClient;
        Dictionary<string, DateTime> _removedChildren = new Dictionary<string, DateTime>();

        public SNSRootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild <SNSRootViewMetaNode>(), accountViewModel, "Amazon SNS")
        {
            this._metaNode = base.MetaNode as SNSRootViewMetaNode;
            this._accountViewModel = accountViewModel;
        }

        public override string ToolTip
        {
            get
            {
                return "Amazon Simple Notification Service (Amazon SNS) is a web service that makes it easy to set up, operate, and send notifications from the cloud. It provides developers with a highly scalable, flexible, and cost-effective capability to publish messages from an application and immediately deliver them to subscribers or other applications. It is designed to make web-scale computing easier for developers.";
            }
        }
        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.SNS.Resources.EmbeddedImages.service-root-icon.png";
            }
        }

        protected override void BuildClient(string accessKey, string secretKey)
        {
            var config = new AmazonSimpleNotificationServiceConfig {ServiceURL = this.CurrentEndPoint.Url};
            if (this.CurrentEndPoint.Signer != null)
                config.SignatureVersion = this.CurrentEndPoint.Signer;
            if (this.CurrentEndPoint.AuthRegion != null)
                config.AuthenticationRegion = this.CurrentEndPoint.AuthRegion;
            this._snsClient = new AmazonSimpleNotificationServiceClient(accessKey, secretKey, config);
        }

        public IAmazonSimpleNotificationService SNSClient
        {
            get { return this._snsClient; }
        }

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

                BeginCopingChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData("ARN", string.Format("arn:aws:sns:{0}:{1}:*",
                this.CurrentEndPoint.RegionSystemName, this.AccountViewModel.AccountNumber));
        }
    }
}
