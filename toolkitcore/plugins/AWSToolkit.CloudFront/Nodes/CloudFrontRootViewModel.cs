using System;
using System.Collections.Generic;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public class CloudFrontRootViewModel : ServiceRootViewModel, ICloudFrontRootViewModel
    {
        private readonly CloudFrontRootViewMetaNode _metaNode;
        private readonly Lazy<IAmazonCloudFront> _cfClient;

        public CloudFrontRootViewModel(AccountViewModel accountViewModel, ToolkitRegion region)
            : base(accountViewModel.MetaNode.FindChild<CloudFrontRootViewMetaNode>(), accountViewModel, "Amazon CloudFront", region)
        {
            _metaNode = base.MetaNode as CloudFrontRootViewMetaNode;
            _cfClient = new Lazy<IAmazonCloudFront>(CreateCloudFrontClient);
        }

        public override string ToolTip =>
            "Amazon CloudFront is a web service for content delivery. " +
            "It integrates with other AWS services to give you an easy way to " +
            "distribute content to end users with low latency and high data transfer speeds.";

        protected override string IconName => AwsImageResourcePath.CloudFront.Path;

        public IAmazonCloudFront CFClient => this._cfClient.Value;

        public ICloudFrontBaseDistributionViewModel AddDistribution(StreamingDistribution distribution)
        {
            var viewModel = new CloudFrontStreamingDistributionViewModel(this._metaNode.CloudFrontStreamingDistributeViewMetaNode, this, distribution);
            this.AddChild(viewModel);

            return viewModel;
        }

        public ICloudFrontBaseDistributionViewModel AddDistribution(Distribution distribution)
        {
            var viewModel = new CloudFrontDistributionViewModel(this._metaNode.CloudFrontDistributeViewMetaNode, this, distribution);
            this.AddChild(viewModel);

            return viewModel;
        }

        public void RemoveDistribution(string name)
        {
            base.RemoveChild(name);
        }

        protected override void LoadChildren()
        {
            List<IViewModel> items = new List<IViewModel>();
            var distRequest = new ListDistributionsRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)distRequest).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

            var response = this.CFClient.ListDistributions(distRequest);
            foreach (var distribution in response.DistributionList.Items)
            {
                var child = new CloudFrontDistributionViewModel(this._metaNode.CloudFrontDistributeViewMetaNode, this, distribution);
                items.Add(child);
            }

            var streamingRequest = new ListStreamingDistributionsRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)streamingRequest).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

            var responseStreaming = this.CFClient.ListStreamingDistributions(streamingRequest);
            foreach (var distribution in responseStreaming.StreamingDistributionList.Items)
            {
                var child = new CloudFrontStreamingDistributionViewModel(this._metaNode.CloudFrontStreamingDistributeViewMetaNode, this, distribution);
                items.Add(child);
            }


            SetChildren(items);
        }

        private IAmazonCloudFront CreateCloudFrontClient()
        {
            return AccountViewModel.CreateServiceClient<AmazonCloudFrontClient>(Region);
        }
    }
}
