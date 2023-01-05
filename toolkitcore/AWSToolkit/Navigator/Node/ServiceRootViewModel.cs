using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;


namespace Amazon.AWSToolkit.Navigator.Node
{
    public abstract class ServiceRootViewModel : InstanceDataRootViewModel, IServiceRootViewModel
    {
        public ServiceRootViewModel(IMetaNode metaNode, IViewModel parent, string name, ToolkitRegion region)
            : this(metaNode, parent, name, region, ToolkitFactory.Instance.ToolkitContext)
        {
        }

        public ServiceRootViewModel(IMetaNode metaNode, IViewModel parent, string name,
            ToolkitRegion region, ToolkitContext toolkitContext)
            : base(metaNode, parent, name, toolkitContext)
        {
            ICredentialIdentifier credentialId = null;

            if (parent is AccountViewModel accountViewModel)
            {
                credentialId = accountViewModel.Identifier;
            }

            AwsConnectionSettings = new AwsConnectionSettings(credentialId, region);
        }

        public ToolkitRegion Region => AwsConnectionSettings.Region;
        public AwsConnectionSettings AwsConnectionSettings { get; }
    }
}
