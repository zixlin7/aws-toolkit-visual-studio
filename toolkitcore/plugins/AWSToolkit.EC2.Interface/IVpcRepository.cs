using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.EC2
{
    public interface IVpcRepository
    {
        Task<ICollection<VpcEntity>> ListVpcsAsync(ICredentialIdentifier credentialsId, ToolkitRegion region);
    }
}
