using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.CloudFormation.Model;
using Amazon.CloudFormation;

namespace Amazon.AWSToolkit.CommonValidators
{
    /// <summary>
    /// Validates whether a CFN stack is in a valid state
    /// </summary>
    public class CfnStackStatusValidator
    {
        private static readonly List<string> _invalidStackStatus = new List<string>()
        {
            StackStatus.DELETE_FAILED,
            StackStatus.UPDATE_ROLLBACK_FAILED,
            StackStatus.ROLLBACK_FAILED,
            StackStatus.UPDATE_FAILED
        };

        public static string Validate(IAmazonCloudFormation cfnClient, string stack)
        {
            try
            {
                var response = cfnClient.DescribeStacks(new DescribeStacksRequest() { StackName = stack });
                var status = response.Stacks.First().StackStatus;

                return _invalidStackStatus.Contains(status) ? $"Stack is in an invalid state: {status}" : null;
            }
            catch (Exception ex)
            {
                // silently swallow any errors(for eg. due to permission issues) with this validation to unblock users in UI
                return null;
            }
        }
    }
}
