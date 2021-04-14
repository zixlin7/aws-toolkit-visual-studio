using Amazon.AWSToolkit.Regions;


namespace Amazon.AWSToolkit.Navigator.Node
{
    public abstract class ServiceRootViewModel : InstanceDataRootViewModel, IServiceRootViewModel
    {
        private readonly ToolkitRegion _region;

        public ServiceRootViewModel(IMetaNode metaNode, IViewModel parent, string name, ToolkitRegion region)
            : base(metaNode, parent, name)
        {
            this._region = region;
        }

        public ToolkitRegion Region => _region;
    }
}
