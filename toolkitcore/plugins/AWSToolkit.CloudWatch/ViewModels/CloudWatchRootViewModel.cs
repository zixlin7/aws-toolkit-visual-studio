using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.CloudWatch.Nodes;
using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.CloudWatch;
using Amazon.CloudWatchLogs;

namespace Amazon.AWSToolkit.CloudWatch.ViewModels
{
    /// <summary>
    /// Backing view model for CloudWatch service node
    /// </summary>
    public class CloudWatchRootViewModel : ServiceRootViewModel
    {
        private readonly Lazy<IAmazonCloudWatch> _cloudWatchClient;
        private readonly Lazy<IAmazonCloudWatchLogs> _cloudWatchLogsClient;
        private readonly ICredentialIdentifier _identifier;

        public CloudWatchRootViewModel(IMetaNode cloudWatchMetaNode, IViewModel parent, ICredentialIdentifier identifier, ToolkitRegion region, ToolkitContext context)
            : base(cloudWatchMetaNode, parent, "Amazon CloudWatch", region, context)
        {
            _identifier = identifier;
            _cloudWatchClient = new Lazy<IAmazonCloudWatch>(CreateCloudWatchClient);
            _cloudWatchLogsClient = new Lazy<IAmazonCloudWatchLogs>(CreateCloudWatchLogsClient);
        }

        public override string ToolTip =>
            "Amazon CloudWatch is a monitoring and observability service built for DevOps engineers, developers, site reliability engineers (SREs), IT managers, and product owners.";

        protected override string IconName => AwsImageResourcePath.CloudWatch.Path;

        public IAmazonCloudWatch CloudWatchClient => _cloudWatchClient.Value;

        public IAmazonCloudWatchLogs CloudWatchLogsClient => _cloudWatchLogsClient.Value;

        public ICredentialIdentifier Identifier => _identifier;

        protected override void LoadChildren()
        {
            try
            {
                var children = new List<IViewModel>
                {
                    new LogGroupsRootViewModel(MetaNode.FindChild<LogGroupsRootViewMetaNode>(), this, ToolkitContext)
                };
                SetChildren(children);
            }
            catch (Exception ex)
            {
                AddErrorChild(ex);
            }
        }

        private IAmazonCloudWatch CreateCloudWatchClient()
        {
            return ToolkitContext.ServiceClientManager.CreateServiceClient<AmazonCloudWatchClient>(_identifier, Region);
        }

        private IAmazonCloudWatchLogs CreateCloudWatchLogsClient()
        {
            return ToolkitContext.ServiceClientManager.CreateServiceClient<AmazonCloudWatchLogsClient>(_identifier, Region);
        }
    }
}
