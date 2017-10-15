using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit
{
    public static class IAMUtilities
    {
        public static Role CreateRole(IAmazonIdentityManagementService iamClient, string baseFunctionName, string assumeRolePolicy)
        {
            var newRoleName = baseFunctionName;
            var existingRoleNames = ExistingRoleNames(iamClient);

            if (existingRoleNames.Contains(newRoleName))
            {
                var baseRoleName = newRoleName;
                for (int i = 0; true; i++)
                {
                    var tempName = baseRoleName + "-" + i;
                    if (!existingRoleNames.Contains(tempName))
                    {
                        newRoleName = tempName;
                        break;
                    }
                }
            }

            var createRequest = new CreateRoleRequest
            {
                RoleName = newRoleName,
                AssumeRolePolicyDocument = assumeRolePolicy
            };
            var createResponse = iamClient.CreateRole(createRequest);
            return createResponse.Role;
        }

        public static HashSet<string> ExistingRoleNames(IAmazonIdentityManagementService iamClient)
        {
            HashSet<string> roles = new HashSet<string>();

            ListRolesResponse response = null;
            do
            {
                ListRolesRequest request = new ListRolesRequest();
                if (response != null)
                    request.Marker = response.Marker;
                response = iamClient.ListRoles(request);
                foreach (var role in response.Roles)
                {
                    roles.Add(role.RoleName);
                }
            } while (response.IsTruncated);

            return roles;
        }

    }
}
