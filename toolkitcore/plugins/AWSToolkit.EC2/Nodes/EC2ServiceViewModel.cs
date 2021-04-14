using System;
using Amazon.EC2;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using log4net;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public abstract class EC2ServiceViewModel : ServiceRootViewModel
    {
        private readonly Lazy<IAmazonEC2> _ec2Client;

        public EC2ServiceViewModel(IMetaNode metaNode, AccountViewModel accountViewModel, string name,
            ToolkitRegion region)
            : base(metaNode, accountViewModel, name, region)
        {
            _ec2Client = new Lazy<IAmazonEC2>(CreateEc2Client);
        }

        public IAmazonEC2 EC2Client => this._ec2Client.Value;

        private IAmazonEC2 CreateEc2Client()
        {
            return AccountViewModel.CreateServiceClient<AmazonEC2Client>(Region);
        }
    }
}
