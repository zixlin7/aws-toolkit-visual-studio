using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.Runtime;
using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class RootViewModel : ServiceRootViewModel, IECSRootViewModel
    {
        RootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;

        private IAmazonECS _ecsClient;
        private IAmazonECR _ecrClient;
        static ILog _logger = LogManager.GetLogger(typeof(ServiceRootViewModel));

        public RootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild<RootViewMetaNode>(), accountViewModel, "Amazon ECS")
        {
            this._metaNode = base.MetaNode as RootViewMetaNode;
            this._accountViewModel = accountViewModel;
        }

        public IAmazonECS ECSClient
        {
            get { return this._ecsClient; }
        }

        public IAmazonECR ECRClient
        {
            get { return this._ecrClient; }
        }

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            var ecsConfig = new AmazonECSConfig { ServiceURL = this.CurrentEndPoint.Url };
            if (this.CurrentEndPoint.Signer != null)
                ecsConfig.SignatureVersion = this.CurrentEndPoint.Signer;
            if (this.CurrentEndPoint.AuthRegion != null)
                ecsConfig.AuthenticationRegion = this.CurrentEndPoint.AuthRegion;
            this._ecsClient = new AmazonECSClient(awsCredentials, ecsConfig);

            var ecrEndpoint = this.CurrentRegion.GetEndpoint(RootViewMetaNode.ECR_ENDPOINT_LOOKUP);
            var ecrConfig = new AmazonECRConfig { ServiceURL = ecrEndpoint.Url };
            if (ecrEndpoint.Signer != null)
                ecrConfig.SignatureVersion = ecrEndpoint.Signer;
            if (ecrEndpoint.AuthRegion != null)
                ecrConfig.AuthenticationRegion = ecrEndpoint.AuthRegion;
            this._ecrClient = new AmazonECRClient(awsCredentials, ecrConfig);

        }

        public override string ToolTip
        {
            get
            {
                return "Amazon EC2 Container Service (Amazon ECS) is a highly scalable, fast, container management service that makes it easy to run, stop, "
                        + "and manage Docker containers on a cluster of EC2 instances. Images may be managed using Amazon EC2 Container Registry (Amazon ECR), "
                        + "a managed AWS Docker registry service";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.service-root-icon.png";
            }
        }


        protected override void LoadChildren()
        {
            try
            {
                List<IViewModel> items = new List<IViewModel>
                {
                    new ClustersRootViewModel(this.MetaNode.FindChild<ClustersRootViewMetaNode>(), this),
                    new RepositoriesRootViewModel(this.MetaNode.FindChild<RepositoriesRootViewMetaNode>(), this),
                };
                BeginCopingChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }
    }
}
