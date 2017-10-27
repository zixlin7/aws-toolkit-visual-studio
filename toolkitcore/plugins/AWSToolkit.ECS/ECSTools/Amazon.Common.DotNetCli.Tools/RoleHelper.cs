using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Auth.AccessControlPolicy;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

namespace Amazon.Common.DotNetCli.Tools
{
    /// <summary>
    /// Utility class for interacting with console user to select or create an IAM role
    /// </summary>
    public class RoleHelper
    {
        public const int DEFAULT_ITEM_MAX = 20;
        private const int MAX_LINE_LENGTH_FOR_MANAGED_ROLE = 95;
        static readonly TimeSpan SLEEP_TIME_FOR_ROLE_PROPOGATION = TimeSpan.FromSeconds(15);
        public IAmazonIdentityManagementService IAMClient { get; private set; }

        public RoleHelper(IAmazonIdentityManagementService iamClient)
        {
            this.IAMClient = iamClient;
        }

        public string GenerateUniqueIAMRoleName(string baseName)
        {
            var existingRoleNames = new HashSet<string>();
            var response = new ListRolesResponse();
            do
            {
                var roles = this.IAMClient.ListRoles(new ListRolesRequest { Marker = response.Marker }).Roles;
                roles.ForEach(x => existingRoleNames.Add(x.RoleName));

            } while (response.IsTruncated);

            if (!existingRoleNames.Contains(baseName))
                return baseName;

            for (int i = 1; true; i++)
            {
                var name = baseName + "-" + i;
                if (!existingRoleNames.Contains(name))
                    return name;
            }
        }

        /*
                public string PromptForRole()
                {
                    var existingRoles = FindExistingECSRoles(DEFAULT_ITEM_MAX);
                    if (existingRoles.Count == 0)
                    {
                        return CreateRole();
                    }

                    var roleArn = SelectFromExisting(existingRoles);
                    return roleArn;
                }

                private string SelectFromExisting(IList<Role> existingRoles)
                {
                    Console.Out.WriteLine("Select IAM Role that will provide AWS credentials to the application:");
                    for (int i = 0; i < existingRoles.Count; i++)
                    {
                        Console.Out.WriteLine($"   {(i + 1).ToString().PadLeft(2)}) {existingRoles[i].RoleName}");
                    }

                    Console.Out.WriteLine($"   {(existingRoles.Count + 1).ToString().PadLeft(2)}) *** Create new IAM Role ***");
                    Console.Out.Flush();

                    int chosenIndex = WaitForIndexResponse(1, existingRoles.Count + 1);

                    if (chosenIndex - 1 < existingRoles.Count)
                    {
                        return existingRoles[chosenIndex - 1].Arn;
                    }
                    else
                    {
                        return CreateRole();
                    }
                }

                private string CreateRole()
                {
                    Console.Out.WriteLine($"Enter name of the new IAM Role:");
                    var roleName = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(roleName))
                        return null;

                    roleName = roleName.Trim();

                    Console.Out.WriteLine("Select IAM Policy to attach to the new role and grant permissions");

                    var managedPolices = FindECSManagedPoliciesAsync(this.IAMClient, DEFAULT_ITEM_MAX).Result;
                    for (int i = 0; i < managedPolices.Count; i++)
                    {
                        var line = $"   {(i + 1).ToString().PadLeft(2)}) {managedPolices[i].PolicyName}";

                        var description = AttemptToGetPolicyDescription(managedPolices[i].Arn);
                        if (!string.IsNullOrEmpty(description))
                        {
                            if ((line.Length + description.Length) > MAX_LINE_LENGTH_FOR_MANAGED_ROLE)
                                description = description.Substring(0, MAX_LINE_LENGTH_FOR_MANAGED_ROLE - line.Length) + " ...";
                            line += $" ({description})";
                        }

                        Console.Out.WriteLine(line);
                    }

                    Console.Out.WriteLine($"   {(managedPolices.Count + 1).ToString().PadLeft(2)}) *** No policy, add permissions later ***");
                    Console.Out.Flush();

                    int chosenIndex = WaitForIndexResponse(1, managedPolices.Count + 1);

                    string managedPolicyArn = null;
                    if (chosenIndex < managedPolices.Count)
                    {
                        var selectedPolicy = managedPolices[chosenIndex - 1];
                        managedPolicyArn = Constants.AWS_MANAGED_POLICY_ARN_PREFIX + selectedPolicy.Path + selectedPolicy.PolicyName;
                    }

                    string roleArn = CreateDefaultRole(roleName, managedPolicyArn);

                    return roleArn;

                }
        */
        private int WaitForIndexResponse(int min, int max)
        {
            int chosenIndex = -1;
            while (chosenIndex == -1)
            {
                var indexInput = Console.ReadLine()?.Trim();
                int parsedIndex;
                if (int.TryParse(indexInput, out parsedIndex) && parsedIndex >= min && parsedIndex <= max)
                {
                    chosenIndex = parsedIndex;
                }
                else
                {
                    Console.Out.WriteLine($"Invalid selection, must be a number between {min} and {max}");
                }
            }

            return chosenIndex;
        }

        private string ExpandRoleName(string roleName)
        {
            return ExpandRoleName(this.IAMClient, roleName);
        }

        public static string ExpandRoleName(IAmazonIdentityManagementService iamClient, string roleName)
        {
            if (roleName.StartsWith("arn:aws"))
                return roleName;

            // Wrapping this in a task to avoid dealing with aggregate exception.
            var task = Task.Run<string>(async () =>
            {
                try
                {
                    var request = new GetRoleRequest { RoleName = roleName };
                    var response = await iamClient.GetRoleAsync(request).ConfigureAwait(false);
                    return response.Role.Arn;
                }
                catch (NoSuchEntityException)
                {
                    return null;
                }

            });

            if(task.Result == null)
            {
                throw new ToolsException($"Role \"{roleName}\" can not be found.", ToolsException.CommonErrorCode.RoleNotFound);
            }

            return task.Result;
        }


        public static string ExpandManagedPolicyName(IAmazonIdentityManagementService iamClient, string managedPolicy)
        {
            if (managedPolicy.StartsWith("arn:aws"))
                return managedPolicy;

            // Wrapping this in a task to avoid dealing with aggregate exception.
            var task = Task.Run<string>(async () =>
            {
                var listResponse = new ListPoliciesResponse();
                do
                {
                    var listRequest = new ListPoliciesRequest { Marker = listResponse.Marker, Scope = PolicyScopeType.All };
                    listResponse = await iamClient.ListPoliciesAsync(listRequest).ConfigureAwait(false);
                    var policy = listResponse.Policies.FirstOrDefault(x => string.Equals(managedPolicy, x.PolicyName));
                    if (policy != null)
                        return policy.Arn;

                } while (listResponse.IsTruncated);

                return null;
            });

            if (task.Result == null)
            {
                throw new ToolsException($"Policy \"{managedPolicy}\" can not be found.", ToolsException.CommonErrorCode.PolicyNotFound);
            }

            return task.Result;
        }

        public string CreateDefaultRole(string roleName, string assuleRolePolicy, string managedPolicy)
        {
            if (!string.IsNullOrEmpty(managedPolicy) && !managedPolicy.StartsWith("arn:aws"))
            {
                managedPolicy = ExpandManagedPolicyName(this.IAMClient, managedPolicy);
            }


            string roleArn;
            try
            {
                CreateRoleRequest request = new CreateRoleRequest
                {
                    RoleName = roleName,
                    AssumeRolePolicyDocument = assuleRolePolicy
                };

                var response = this.IAMClient.CreateRoleAsync(request).Result;
                roleArn = response.Role.Arn;
            }
            catch (Exception e)
            {
                throw new ToolsException($"Error creating IAM Role: {e.Message}", ToolsException.CommonErrorCode.IAMCreateRole, e);
            }

            if (!string.IsNullOrEmpty(managedPolicy))
            {
                try
                {
                    var request = new AttachRolePolicyRequest
                    {
                        RoleName = roleName,
                        PolicyArn = managedPolicy
                    };
                    this.IAMClient.AttachRolePolicyAsync(request).Wait();
                }
                catch (Exception e)
                {
                    throw new ToolsException($"Error assigning managed IAM Policy: {e.Message}", ToolsException.CommonErrorCode.IAMAttachRole, e);
                }
            }

            bool found = false;
            do
            {
                // There is no way check if the role has propagted yet so to
                // avoid error during deployment creation do a generous sleep.
                Console.WriteLine("Waiting for new IAM Role to propagate to AWS regions");
                long start = DateTime.Now.Ticks;
                while (TimeSpan.FromTicks(DateTime.Now.Ticks - start).TotalSeconds < SLEEP_TIME_FOR_ROLE_PROPOGATION.TotalSeconds)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Console.Write(".");
                    Console.Out.Flush();
                }
                Console.WriteLine("\t Done");


                try
                {
                    var getResponse = this.IAMClient.GetRoleAsync(new GetRoleRequest { RoleName = roleName }).Result;
                    if (getResponse.Role != null)
                        found = true;
                }
                catch (NoSuchEntityException)
                {

                }
                catch (Exception e)
                {
                    throw new ToolsException("Error confirming new role was created: " + e.Message, ToolsException.CommonErrorCode.IAMGetRole, e);
                }
            } while (!found);


            return roleArn;
        }

        public static async Task<IList<ManagedPolicy>> FindECSManagedPoliciesAsync(IAmazonIdentityManagementService iamClient, int maxPolicies)
        {
            ListPoliciesRequest request = new ListPoliciesRequest
            {
                Scope = PolicyScopeType.AWS,
            };
            ListPoliciesResponse response = null;

            IList<ManagedPolicy> ecsPolicies = new List<ManagedPolicy>();
            do
            {
                request.Marker = response?.Marker;
                response = await iamClient.ListPoliciesAsync(request).ConfigureAwait(false);

                foreach (var policy in response.Policies)
                {
                    if (policy.IsAttachable && KNOWN_MANAGED_POLICY_DESCRIPTIONS.ContainsKey(policy.PolicyName))
                        ecsPolicies.Add(policy);

                    if (ecsPolicies.Count == maxPolicies)
                        return ecsPolicies;
                }

            } while (response.IsTruncated);

            response = await iamClient.ListPoliciesAsync(new ListPoliciesRequest
            {
                Scope = PolicyScopeType.Local
            });

            foreach (var policy in response.Policies)
            {
                if (policy.IsAttachable)
                    ecsPolicies.Add(policy);

                if (ecsPolicies.Count == maxPolicies)
                    return ecsPolicies;
            }


            return ecsPolicies;
        }

        public static async Task<IList<Role>> FindExistingECSRolesAsync(IAmazonIdentityManagementService iamClient, int maxRoles)
        {
            List<Role> roles = new List<Role>();

            ListRolesRequest request = new ListRolesRequest();
            ListRolesResponse response = null;
            do
            {
                if (response != null)
                    request.Marker = response.Marker;

                response = await iamClient.ListRolesAsync(request).ConfigureAwait(false);

                foreach (var role in response.Roles)
                {
                    if (AssumeRoleServicePrincipalSelector(role, "ecs-tasks.amazonaws.com"))
                    {
                        roles.Add(role);
                        if (roles.Count == maxRoles)
                        {
                            break;
                        }
                    }
                }

            } while (response.IsTruncated && roles.Count < maxRoles);

            return roles;
        }

        private IList<Role> FindExistingECSRoles(int maxRoles)
        {
            var task = Task.Run<IList<Role>>(async () =>
            {
                return await FindExistingECSRolesAsync(this.IAMClient, maxRoles);
            });

            return task.Result;
        }

        public static bool AssumeRoleServicePrincipalSelector(Role r, string servicePrincipal)
        {
            if (string.IsNullOrEmpty(r.AssumeRolePolicyDocument))
                return false;

            try
            {
                var decode = WebUtility.UrlDecode(r.AssumeRolePolicyDocument);
                var policy = Policy.FromJson(decode);
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
            catch (Exception)
            {
                return false;
            }
        }


        static readonly Dictionary<string, string> KNOWN_MANAGED_POLICY_DESCRIPTIONS = new Dictionary<string, string>
        {
            {"PowerUserAccess","Provides full access to AWS services and resources, but does not allow management of users and groups."},
            {"AmazonS3FullAccess","Provides full access to all buckets via the AWS Management Console."},
            {"AmazonDynamoDBFullAccess","Provides full access to Amazon DynamoDB via the AWS Management Console."},
            {"CloudWatchLogsFullAccess","Provides full access to CloudWatch Logs"}
        };

        /// <summary>
        /// Because description does not come back in the list policy operation cache known policy descriptions to 
        /// help users understand which role to pick.
        /// </summary>
        /// <param name="policyArn"></param>
        /// <returns></returns>
        public string AttemptToGetPolicyDescription(string policyArn)
        {
            string content;
            if (!KNOWN_MANAGED_POLICY_DESCRIPTIONS.TryGetValue(policyArn, out content))
                return null;

            return content;
        }
    }
}
