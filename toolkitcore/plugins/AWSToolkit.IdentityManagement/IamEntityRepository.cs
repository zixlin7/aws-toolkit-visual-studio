using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.IdentityManagement.Models;
using Amazon.AWSToolkit.Regions;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.IdentityManagement
{
    public class IamEntityRepository : IIamEntityRepository
    {
        private readonly ToolkitContext _toolkitContext;

        public IamEntityRepository(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public async Task<ICollection<IamRole>> ListIamRolesAsync(ICredentialIdentifier credentialsId, ToolkitRegion region)
        {
            var iam = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonIdentityManagementServiceClient>(credentialsId, region);

            var roles = new List<IamRole>();

            var request = new ListRolesRequest();
            do
            {
                var response = await iam.ListRolesAsync(request).ConfigureAwait(false);
                request.Marker = response.Marker;

                roles.AddRange(response.Roles.Select(r => new IamRole()
                {
                    Id = r.RoleId,
                    Name = r.RoleName,
                    Arn = r.Arn,
                    AssumeRolePolicyDocument = r.AssumeRolePolicyDocument,
                }));
            } while (!string.IsNullOrWhiteSpace(request.Marker));

            return roles;
        }
    }
}
