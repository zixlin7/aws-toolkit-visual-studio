using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.EC2;
using Amazon.EC2.Model;
using log4net;

namespace Amazon.AWSToolkit.EC2
{
    public static class EC2Utilities
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(EC2Utilities));

        public static string TruncateSelectedItemsName(string names)
        {
            if (names.Length > EC2Constants.SELECTED_ITEMS_NAME_LENGTH)
                names = names.Substring(0, EC2Constants.SELECTED_ITEMS_NAME_LENGTH) + "...";

            return names;
        }

        public static bool checkIfPortOpen(IAmazonEC2 ec2Client, List<GroupIdentifier> securityGroups, int port)
        {
            var groupIds = new List<string>();
            securityGroups.ForEach(x => groupIds.Add(x.GroupId));
            var request = new DescribeSecurityGroupsRequest() { GroupIds = groupIds };
            var response = ec2Client.DescribeSecurityGroups(request);

            // If we can't find the group then situation is undefined so we will ignore it
            // and return true
            if (response.SecurityGroups.Count == 0)
                return true;

            foreach (var group in response.SecurityGroups)
            {
                foreach (var rule in group.IpPermissions)
                {
                    if (rule.UserIdGroupPairs != null && rule.UserIdGroupPairs.Count > 0)
                        continue;

                    int fromPort = (int)rule.FromPort;
                    int toPort = (int)rule.ToPort;

                    if (fromPort == port)
                        return true;
                    if (fromPort < port && port <= toPort)
                        return true;
                }
            }

            return false;
        }

        public static bool CheckForVpcOnlyMode(IAmazonEC2 ec2Client)
        {
            try
            {
                var response = ec2Client.DescribeAccountAttributes(new DescribeAccountAttributesRequest
                {
                    AttributeNames = new List<string>
                {
                    "supported-platforms"
                }
                });

                return response.AccountAttributes[0].AttributeValues.All(v => !v.AttributeValue.Equals("EC2", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("CheckForVpcOnlyMode failed with exception {0}, assuming 'true'", e.Message);
            }

            // fallback to assumption of true, since all new accounts and regions are vpc only
            return true;
        }
    }
}
