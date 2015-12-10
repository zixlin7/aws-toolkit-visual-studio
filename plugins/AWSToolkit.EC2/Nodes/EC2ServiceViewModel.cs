using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Threading;

using Amazon.EC2;
using Amazon.EC2.Model;

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

        public IAmazonEC2 EC2Client
        {
            get
            {
                return this._ec2Client;
            }
        }

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            var config = new AmazonEC2Config {ServiceURL = this.CurrentEndPoint.Url};
            if (this.CurrentEndPoint.Signer != null)
                config.SignatureVersion = this.CurrentEndPoint.Signer;
            if (this.CurrentEndPoint.AuthRegion != null)
                config.AuthenticationRegion = this.CurrentEndPoint.AuthRegion;
            this._ec2Client = new AmazonEC2Client(awsCredentials, config);
        }
    }
}
