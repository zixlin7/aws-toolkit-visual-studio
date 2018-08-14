using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using ICSharpCode.SharpZipLib.Zip;
using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.Lambda
{
    public static class LambdaUtilities
    {
        public const int MIN_MEMORY_SIZE = 128;
        public const int MAX_MEMORY_SIZE = 3008;

        public const int MIN_TIMEOUT = 1;
        public const int MAX_TIMEOUT = 300;

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
            for(int i = MIN_MEMORY_SIZE; i <= MAX_MEMORY_SIZE; i = i + 64)
            {
                values.Add(i);
            }

            return values.ToArray();
        }


        public enum RoleType {Kinesis, DynamoDBStream, SQS};

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
            if (type == RoleType.DynamoDBStream)
            {
                using (var reader = new System.IO.StreamReader(assembly.GetManifestResourceStream("Amazon.AWSToolkit.Lambda.Policies.LambdaInvokeRoleDynamoDBStream.json")))
                    policy = reader.ReadToEnd().Trim();

                policyName = "DynamoDBStream-Read-for-Lambda";
            }
            if (type == RoleType.SQS)
            {
                using (var reader = new System.IO.StreamReader(assembly.GetManifestResourceStream("Amazon.AWSToolkit.Lambda.Policies.LambdaInvokeRoleSQS.json")))
                    policy = reader.ReadToEnd().Trim();

                policyName = "SQS-Read-for-Lambda";
            }

            iamClient.PutRolePolicy(new PutRolePolicyRequest
            {
                RoleName = role,
                PolicyDocument = policy,
                PolicyName = policyName
            });
        }


        #region Code to remove invalid accounts that were added IAM Roles created by the toolkit for Lambda deployment.

        static readonly string[] invalidAccounts = new string[] { "arn:aws:iam::771762743097:root", "arn:aws:iam::571267556732:root", "arn:aws:iam::147242972042:root" };
        public static bool DoesAssumeRolePolicyDocumentContainsInvalidAccounts(string assumeRolePolicy)
        {
            foreach (var invalidAccount in invalidAccounts)
            {
                if (assumeRolePolicy.Contains(invalidAccount))
                {
                    return true;
                }
            }

            return false;
        }

        public static string RemoveInvalidAccountsFromAssumeRolePolicyDocument(string assumeRolePolicy)
        {
            const string STATEMENT_KEY = "Statement";
            const string PRINCIPAL_KEY = "Principal";
            const string AWS_KEY = "AWS";

            var rootData = JsonMapper.ToObject(assumeRolePolicy) as JsonData;
            var statements = rootData[STATEMENT_KEY] as JsonData;

            // Collect the valid statements in the policy
            var validStatements = new List<JsonData>();
            foreach (JsonData statement in statements)
            {
                // If it is not an AWS principal like a service then assume it is valid.
                var principal = statement[PRINCIPAL_KEY] as JsonData;
                if (principal == null || principal[AWS_KEY] == null)
                {
                    validStatements.Add(statement);
                    continue;
                }

                // If it is an AWS principal make sure the principal isn't one of our known bad principal accounts. If it is not
                // then assume it is a valid statement.
                var account = principal[AWS_KEY].ToString();
                if(!invalidAccounts.Contains(account))
                {
                    validStatements.Add(statement);
                    continue;
                }
            }

            // Replace the existing statements with the now determined good statements.
            rootData[STATEMENT_KEY].Clear();
            foreach(var statement in validStatements)
            {
                rootData[STATEMENT_KEY].Add(statement);
            }

            return Utility.PrettyPrintJson(rootData);
        }
        #endregion

    }
}
