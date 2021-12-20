using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
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

        public async Task<ICollection<string>> ListIamRoleArnsAsync(ICredentialIdentifier credentialsId, ToolkitRegion region)
        {
            var iam = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonIdentityManagementServiceClient>(credentialsId, region);

            var arns = new List<string>();

            var request = new ListRolesRequest();
            do
            {
                var response = await iam.ListRolesAsync(request).ConfigureAwait(false);
                request.Marker = response.Marker;

                arns.AddRange(response.Roles.Select(r => r.Arn));
            } while (!string.IsNullOrWhiteSpace(request.Marker));

            return arns;
        }
    }
}
