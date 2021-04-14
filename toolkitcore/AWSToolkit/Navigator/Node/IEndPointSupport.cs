using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public interface IEndPointSupport : IViewModel
    {
        ToolkitRegion Region { get; }
    }
}
