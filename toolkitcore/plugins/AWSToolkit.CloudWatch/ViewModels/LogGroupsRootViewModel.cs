using Amazon.AWSToolkit.CloudWatch.Nodes;
using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.CloudWatchLogs;

namespace Amazon.AWSToolkit.CloudWatch.ViewModels
{
    /// <summary>
    /// Backing view model for CloudWatch log groups resource node
    /// </summary>
    public class LogGroupsRootViewModel : AbstractViewModel
    {
        private readonly IAmazonCloudWatchLogs _cloudWatchLogsClient;

        public LogGroupsRootViewModel(LogGroupsRootViewMetaNode metaNode, CloudWatchRootViewModel viewModel,
            ToolkitContext toolkitContext)
            : base(metaNode, viewModel, "Log Groups", toolkitContext)
        {
            _cloudWatchLogsClient = viewModel.CloudWatchLogsClient;
        }

        public IAmazonCloudWatchLogs CloudWatchLogsClient => _cloudWatchLogsClient;

        public ToolkitRegion Region
        {
            get
            {
                IEndPointSupport support = this.Parent as IEndPointSupport;
                return support.Region;
            }
        }

        protected override string IconName => AwsImageResourcePath.CloudWatchLogGroups.Path;
    }
}
