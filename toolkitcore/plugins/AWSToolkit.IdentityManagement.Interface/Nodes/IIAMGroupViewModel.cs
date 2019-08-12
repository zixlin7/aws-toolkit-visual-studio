using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public interface IIAMGroupViewModel
    {
        IAmazonIdentityManagementService IAMClient { get; }

        Group Group { get; }
        void UpdateGroup(string groupName);
    }
}
