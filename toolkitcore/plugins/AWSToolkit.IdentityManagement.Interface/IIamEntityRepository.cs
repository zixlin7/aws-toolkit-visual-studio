using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.IdentityManagement.Models;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.IdentityManagement
{
    public interface IIamEntityRepository
    {
        Task<ICollection<IamRole>> ListIamRolesAsync(ICredentialIdentifier credentialsId, ToolkitRegion region);
    }
}
