using Amazon.IdentityManagement;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public interface IIAMRootViewModel : IServiceRootViewModel
    {
        IAmazonIdentityManagementService IAMClient { get; }

    }
}
