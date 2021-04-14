using System;
using Amazon.AWSToolkit.Context;

namespace AWSDeployment
{
    public static class DeploymentEngineFactory
    {
        public static readonly string ElasticBeanstalkServiceName = "ElasticBeanstalk";
        public static readonly string CloudFormationServiceName = "CloudFormation";

        /// <summary>
        /// Returns a deployment engine instance suitable for use with the specified service.
        /// </summary>
        /// <param name="forService">"ElasticBeanstalk" or "CloudFormation"</param>
        public static DeploymentEngineBase CreateEngine(string forService, ToolkitContext toolkitContext)
        {
            if (string.Compare(forService, ElasticBeanstalkServiceName, true) == 0)
                return new BeanstalkDeploymentEngine(toolkitContext.RegionProvider);
            
            if (string.Compare(forService, CloudFormationServiceName, true) == 0)
                return new CloudFormationDeploymentEngine();

            throw new ArgumentException("Unknown service; no matching deployment engine available - " + forService);
        }
    }
}
