using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.CloudFront.Nodes;
using Amazon.AWSToolkit.CloudFront.Controller;

namespace Amazon.AWSToolkit.CloudFront
{
    public class CloudFrontActivator : AbstractPluginActivator
    {
        public override string PluginName
        {
            get { return "CloudFront"; }
        }

        public override void RegisterMetaNodes()
        {
            var rootMetaNode = new CloudFrontRootViewMetaNode();
            var distributionMetaNode = new CloudFrontDistributeViewMetaNode();
            var streamingDistributionMetaNode = new CloudFrontStreamingDistributeViewMetaNode();

            rootMetaNode.Children.Add(distributionMetaNode);
            rootMetaNode.Children.Add(streamingDistributionMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);    
        }

        void setupContextMenuHooks(CloudFrontRootViewMetaNode rootNode)
        {
            rootNode.OnCreateDistribution =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<CreateDistributionController>().Execute);

            rootNode.OnCreateStreamingDistribution =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<CreateStreamingDistributionController>().Execute);

            rootNode.OnViewOriginAccessIdentities =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewOriginAccessIdentiesController>().Execute);

            rootNode.CloudFrontDistributeViewMetaNode.OnProperties =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<PropertiesController>().Execute);

            rootNode.CloudFrontDistributeViewMetaNode.OnDeleteDistribution =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteDistributionController>().Execute);

            rootNode.CloudFrontDistributeViewMetaNode.OnViewInvalidationRequests =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewInvalidationRequestsController>().Execute);


            rootNode.CloudFrontStreamingDistributeViewMetaNode.OnProperties =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<PropertiesController>().Execute);

            rootNode.CloudFrontStreamingDistributeViewMetaNode.OnDeleteDistribution =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteDistributionController>().Execute);
        }
    }
}
