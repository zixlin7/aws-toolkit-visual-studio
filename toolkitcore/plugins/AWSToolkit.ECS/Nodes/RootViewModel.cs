﻿using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.ECS;
using Amazon.ECR;
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
            : base(accountViewModel.MetaNode.FindChild<RootViewMetaNode>(), accountViewModel, "Amazon Elastic Container Service")
        {
            this._metaNode = base.MetaNode as RootViewMetaNode;
            this._accountViewModel = accountViewModel;
        }

        public IAmazonECS ECSClient => this._ecsClient;

        public IAmazonECR ECRClient => this._ecrClient;

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            var ecsConfig = new AmazonECSConfig();
            this.CurrentEndPoint.ApplyToClientConfig(ecsConfig);
            this._ecsClient = new AmazonECSClient(awsCredentials, ecsConfig);

            var ecrEndpoint = this.CurrentRegion.GetEndpoint(RootViewMetaNode.ECR_ENDPOINT_LOOKUP);
            var ecrConfig = new AmazonECRConfig();
            ecrEndpoint.ApplyToClientConfig(ecrConfig);
            this._ecrClient = new AmazonECRClient(awsCredentials, ecrConfig);

        }

        public override string ToolTip =>
            "Amazon Elastic Container Service (Amazon ECS) is a highly scalable, fast, container management service that makes it easy to run, stop, "
            + "and manage Docker containers. Images may be managed using Amazon Elastic Container Registry (Amazon ECR), "
            + "a managed AWS Docker registry service";

        protected override string IconName => "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.service-root-icon.png";


        protected override void LoadChildren()
        {
            try
            {
                List<IViewModel> items = new List<IViewModel>
                {
                    new ClustersRootViewModel(this.MetaNode.FindChild<ClustersRootViewMetaNode>(), this),
                    //new TaskDefinitionsRootViewModel(this.MetaNode.FindChild<TaskDefinitionsRootViewMetaNode>(), this),
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
