using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.IdentityManagement
{
    public interface IIamEntityRepository
    {
        Task<ICollection<string>> ListIamRoleArnsAsync(ICredentialIdentifier credentialsId, ToolkitRegion region);
    }
}
