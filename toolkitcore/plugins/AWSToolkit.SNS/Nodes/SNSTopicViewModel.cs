using System.Windows;
using Amazon.SimpleNotificationService;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CommonUI.Images;

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

        protected override string IconName => AwsImageResourcePath.SnsTopic.Path;

        public SNSRootViewModel SNSRootViewModel => this._snsRootViewModel;

        public IAmazonSimpleNotificationService SNSClient => this._snsRootViewModel.SNSClient;

        public string TopicArn => this._topicArn;

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.TopicArn);
            dndDataObjects.SetData("ARN", this.TopicArn);
        }
    }
}
