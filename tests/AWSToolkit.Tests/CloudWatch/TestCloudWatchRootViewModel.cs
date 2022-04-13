using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;

namespace AWSToolkit.Tests.CloudWatch
{
    /// <summary>
    /// Test only derived class of CloudWatchRootViewModel
    /// </summary>
    public class TestCloudWatchRootViewModel : CloudWatchRootViewModel
    {
        public TestCloudWatchRootViewModel(IMetaNode cloudWatchMetaNode, IViewModel accountViewModel,
            ICredentialIdentifier identifier, ToolkitRegion region, ToolkitContext context) : base(cloudWatchMetaNode,
            accountViewModel, identifier, region, context)
        {
        }

        /// <summary>
        /// Exposes base class protected method which loads children
        /// </summary>
        public void ExposedLoadChildren()
        {
            base.LoadChildren();
        }
    }
}
