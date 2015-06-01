using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public static DeploymentEngineBase CreateEngine(string forService)
        {
            if (string.Compare(forService, ElasticBeanstalkServiceName, true) == 0)
                return new BeanstalkDeploymentEngine();
            
            if (string.Compare(forService, CloudFormationServiceName, true) == 0)
                return new CloudFormationDeploymentEngine();

            throw new ArgumentException("Unknown service; no matching deployment engine available - " + forService);
        }
    }
}
