using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SNS.Nodes
{
    public class SNSTopicViewModel : AbstractViewModel, ISNSTopicViewModel
    {
        SNSTopicViewMetaNode _metaNode;
        SNSRootViewModel _snsRootViewModel;
        string _topicArn;

        public SNSTopicViewModel(SNSTopicViewMetaNode metaNode, SNSRootViewModel snsRootViewModel, string topicArn)
            : base(metaNode, snsRootViewModel, topicArn.Substring(topicArn.LastIndexOf(':') + 1))
        {
            this._metaNode = metaNode;
            this._snsRootViewModel = snsRootViewModel;
            this._topicArn = topicArn;
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.SNS.Resources.EmbeddedImages.topic-node.png";
            }
        }

        public SNSRootViewModel SNSRootViewModel
        {
            get { return this._snsRootViewModel; }
        }

        public IAmazonSimpleNotificationService SNSClient
        {
            get { return this._snsRootViewModel.SNSClient; }
        }

        public string TopicArn
        {
            get { return this._topicArn; }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.TopicArn);
            dndDataObjects.SetData("ARN", this.TopicArn);
        }
    }
}
