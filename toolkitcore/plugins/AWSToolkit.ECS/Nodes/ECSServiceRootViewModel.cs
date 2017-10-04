using Amazon.ECS;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using log4net;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public abstract class ECSServiceRootViewModel : ServiceRootViewModel
    {
        IAmazonECS _ecsClient;
        static ILog _logger = LogManager.GetLogger(typeof(ECSServiceRootViewModel));

        public ECSServiceRootViewModel(IMetaNode metaNode, AccountViewModel accountViewModel, string name)
            : base(metaNode, accountViewModel, name)
        {
        }

        public IAmazonECS ECSClient
        {
            get
            {
                return this._ecsClient;
            }
        }

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            var config = new AmazonECSConfig { ServiceURL = this.CurrentEndPoint.Url };
            if (this.CurrentEndPoint.Signer != null)
                config.SignatureVersion = this.CurrentEndPoint.Signer;
            if (this.CurrentEndPoint.AuthRegion != null)
                config.AuthenticationRegion = this.CurrentEndPoint.AuthRegion;
            this._ecsClient = new AmazonECSClient(awsCredentials, config);
        }
    }
}
