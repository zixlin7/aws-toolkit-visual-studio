using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Regions;


namespace Amazon.AWSToolkit.Navigator.Node
{
    public abstract class ServiceRootViewModel : InstanceDataRootViewModel, IServiceRootViewModel
    {
        private readonly ToolkitRegion _region;

        public ServiceRootViewModel(IMetaNode metaNode, IViewModel parent, string name, ToolkitRegion region)
            : this(metaNode, parent, name, region, ToolkitFactory.Instance.ToolkitContext)
        {
        }

        public ServiceRootViewModel(IMetaNode metaNode, IViewModel parent, string name,
            ToolkitRegion region, ToolkitContext toolkitContext)
            : base(metaNode, parent, name, toolkitContext)
        {
            this._region = region;
        }

        public ToolkitRegion Region => _region;
    }
}
