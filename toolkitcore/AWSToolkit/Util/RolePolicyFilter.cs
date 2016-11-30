using System;
using System.Collections.Generic;
using System.Web;

using Amazon.IdentityManagement.Model;
using Amazon.Auth.AccessControlPolicy;
using log4net;

namespace Amazon.AWSToolkit
{
    public abstract class RolePolicyFilter
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(RolePolicyFilter));

        /// <summary>
        /// Performs a filtering on a set of roles to emit the set that have an assume role 
        /// policy for the specified service principal.
        /// </summary>
        /// <param name="roles">The set of roles to filter.</param>
        /// <param name="servicePrincipal">
        /// The service principal for which an assume role policy has been granted.
        /// </param>
        /// <returns>The set of roles with an assume role policy for the service principal.</returns>
        public static IEnumerable<Role> FilterByAssumeRoleServicePrincipal(IEnumerable<Role> roles, string servicePrincipal)
        {
            var selectedRoles = new List<Role>();

            Func<Role, string, bool> selector = AssumeRoleServicePrincipalSelector;
            foreach (var role in roles)
            {
                if (selector(role, servicePrincipal))
                    selectedRoles.Add(role);
            }

            return selectedRoles;
        }

        public static bool AssumeRoleServicePrincipalSelector(Role r, string servicePrincipal)
        {
            if (string.IsNullOrEmpty(r.AssumeRolePolicyDocument))
                return false;

            try
            {
                var policy = Policy.FromJson(HttpUtility.UrlDecode(r.AssumeRolePolicyDocument));
                foreach (var statement in policy.Statements)
                {
                    if (statement.Actions.Contains(new ActionIdentifier("sts:AssumeRole")) &&
                        statement.Principals.Contains(new Principal("Service", servicePrincipal)))
                    {
                        return true;
                    }
                }
                return r.AssumeRolePolicyDocument.Contains(servicePrincipal);
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("Error parsing assume role document: {0} Error: {1}", r.AssumeRolePolicyDocument, e.Message);
                return false;
            }
        }
    }
}
