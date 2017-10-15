using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.ECS
{

    public static class Constants
    {
        public enum DeployMode { PushOnly, DeployToECSCluster }

        public static readonly string ECS_ASSUME_ROLE_POLICY =
@"
{
  ""Version"": ""2008-10-17"",
  ""Statement"": [
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""Service"": ""ecs.amazonaws.com""
      },
      ""Action"": ""sts:AssumeRole""
    }
  ]
}
".Trim();

        public static readonly string ECS_DEFAULT_SERVICE_POLICY =
@"
{
    ""Version"": ""2012-10-17"",
    ""Statement"": [
        {
            ""Effect"": ""Allow"",
            ""Action"": [
                ""ec2:AuthorizeSecurityGroupIngress"",
                ""ec2:Describe*"",
                ""elasticloadbalancing:DeregisterInstancesFromLoadBalancer"",
                ""elasticloadbalancing:DeregisterTargets"",
                ""elasticloadbalancing:Describe*"",
                ""elasticloadbalancing:RegisterInstancesWithLoadBalancer"",
                ""elasticloadbalancing:RegisterTargets""
            ],
            ""Resource"": ""*""
        }
    ]
}
".Trim();
    }
}
