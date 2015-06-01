using System.Collections.Generic;

using Amazon.CloudFormation.Model;
using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancing.Model;

namespace Amazon.CloudFormation
{
    public static class AmazonCloudFormationClientExt
    {
        public static List<StackResource> GetStackResources(IAmazonCloudFormation cfClient, string stackName)
        {
            var resourcesRequest = new DescribeStackResourcesRequest() { StackName = stackName };
            var resourcesResponse = cfClient.DescribeStackResources(resourcesRequest);
            return resourcesResponse.StackResources;
        }
        
        public static HashSet<string> GetListOfInstanceIdsForStack(IAmazonAutoScaling asClient,
                                                                   IAmazonElasticLoadBalancing elbClient,
                                                                   List<StackResource> stackResources,
                                                                   Dictionary<string, object> fetchedDescribes)
        {
            var instanceIds = new HashSet<string>();
            foreach (var stackResource in stackResources)
            {
                if (stackResource.ResourceType == "AWS::EC2::Instance")
                {
                    if (!string.IsNullOrEmpty(stackResource.PhysicalResourceId) && !instanceIds.Contains(stackResource.PhysicalResourceId))
                        instanceIds.Add(stackResource.PhysicalResourceId);
                }
                else if (stackResource.ResourceType == "AWS::AutoScaling::AutoScalingGroup")
                {
                    var describeGroupRequest = new DescribeAutoScalingGroupsRequest
                    {
                        AutoScalingGroupNames = new List<string> { stackResource.PhysicalResourceId }
                    };
                    var describeGroupResponse = asClient.DescribeAutoScalingGroups(describeGroupRequest);
                    fetchedDescribes[stackResource.LogicalResourceId] = describeGroupResponse;

                    if (describeGroupResponse.AutoScalingGroups.Count != 1)
                        continue;

                    foreach (var instance in describeGroupResponse.AutoScalingGroups[0].Instances)
                    {
                        if (!instanceIds.Contains(instance.InstanceId))
                            instanceIds.Add(instance.InstanceId);
                    }
                }
                else if (stackResource.ResourceType == "AWS::ElasticLoadBalancing::LoadBalancer")
                {
                    var describeLoadRequest = new DescribeLoadBalancersRequest() { LoadBalancerNames = new List<string>() { stackResource.PhysicalResourceId } };
                    var describeLoadResponse = elbClient.DescribeLoadBalancers(describeLoadRequest);
                    fetchedDescribes[stackResource.LogicalResourceId] = describeLoadResponse;

                    if (describeLoadResponse.LoadBalancerDescriptions.Count != 1)
                    {
                        foreach (var instance in describeLoadResponse.LoadBalancerDescriptions[0].Instances)
                        {
                            if (!instanceIds.Contains(instance.InstanceId))
                                instanceIds.Add(instance.InstanceId);
                        }
                    }
                }
            }

            return instanceIds;
        }
    }
}
