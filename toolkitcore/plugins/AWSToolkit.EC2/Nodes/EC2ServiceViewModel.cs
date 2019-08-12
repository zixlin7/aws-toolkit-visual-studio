using Amazon.EC2;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using log4net;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public abstract class EC2ServiceViewModel : ServiceRootViewModel
    {
        IAmazonEC2 _ec2Client;
        static ILog _logger = LogManager.GetLogger(typeof(EC2ServiceViewModel));

        public EC2ServiceViewModel(IMetaNode metaNode, AccountViewModel accountViewModel, string name)
            : base(metaNode, accountViewModel, name)
        {
        }

        public IAmazonEC2 EC2Client => this._ec2Client;

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            var config = new AmazonEC2Config();
            this.CurrentEndPoint.ApplyToClientConfig(config);
            this._ec2Client = new AmazonEC2Client(awsCredentials, config);
        }
    }
}
