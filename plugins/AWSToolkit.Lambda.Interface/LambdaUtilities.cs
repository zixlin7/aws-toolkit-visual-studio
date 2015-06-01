﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using ICSharpCode.SharpZipLib.Zip;

namespace Amazon.AWSToolkit.Lambda
{
    public static class LambdaUtilities
    {
        public static string DetermineStartupFromPath(string sourcePath)
        {
            if (string.Equals(Path.GetExtension(sourcePath), ".js", StringComparison.InvariantCultureIgnoreCase))
            {
                return Path.GetFileName(sourcePath);
            }
            else if (string.Equals(Path.GetExtension(sourcePath), ".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                var zip = new ZipFile(sourcePath);

                var listOfJavascriptFiles = new List<string>();
                foreach (ZipEntry entry in zip)
                {
                    if (!entry.IsFile || entry.Name.Contains('/'))
                        continue;

                    if (string.Equals(entry.Name, "app.js", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return entry.Name;
                    }
                    else if (string.Equals(Path.GetExtension(entry.Name), ".js", StringComparison.InvariantCultureIgnoreCase))
                    {
                        listOfJavascriptFiles.Add(entry.Name);
                    }
                }

                if (listOfJavascriptFiles.Count == 1)
                    return listOfJavascriptFiles[0];
            }
            else if(Directory.Exists(sourcePath))
            {
                var listOfJavascriptFiles = new List<string>();
                foreach (var fullFilePath in Directory.GetFiles(sourcePath))
                {
                    var fileName = Path.GetFileName(fullFilePath);
                    if (string.Equals(fileName, "app.js", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return fileName;
                    }
                    else if (string.Equals(Path.GetExtension(fileName), ".js", StringComparison.InvariantCultureIgnoreCase))
                    {
                        listOfJavascriptFiles.Add(fileName);
                    }
                }

                if (listOfJavascriptFiles.Count == 1)
                    return listOfJavascriptFiles[0];
            }

            return null;
        }


        public static int[] GetValidMemorySizes()
        {
            List<int> values = new List<int>();
            for(int i = 64; i <= 1024; i = i + 64)
            {
                values.Add(i);
            }

            return values.ToArray();
        }

        public static int[] GetValidValuesForTimeout()
        {
            int[] values = new int[60];
            for(int i = 0; i < values.Length; i++)
            {
                values[i] = i + 1;
            }

            return values;
        }

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


        public enum RoleType {Kinesis};

        public static void ApplyPolicyToRole(IAmazonIdentityManagementService iamClient,  RoleType type, string role)
        {
            // The role coming in is an ARN. Get the name only part.
            if (role.Contains("/"))
            {
                role = role.Substring(role.IndexOf("/") + 1);
            }
            string policyName = null;
            string policy = null;
            var assembly = typeof(LambdaUtilities).Assembly;
            if (type == RoleType.Kinesis)
            {
                using (var reader = new System.IO.StreamReader(assembly.GetManifestResourceStream("Amazon.AWSToolkit.Lambda.Policies.LambdaInvokeRoleKinesis.json")))
                    policy = reader.ReadToEnd().Trim();

                policyName = "Kinesis-Read-for-Lambda";
            }

            iamClient.PutRolePolicy(new PutRolePolicyRequest
            {
                RoleName = role,
                PolicyDocument = policy,
                PolicyName = policyName
            });
        }
    }
}
