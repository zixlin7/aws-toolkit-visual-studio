using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.ElasticBeanstalk
{
    public static class BeanstalkConstants
    {
        public const string GIT_PUSH_SERVICE_NAME = "GitPush";

        public const string STATUS_LAUNCHING = "Launching";
        public const string STATUS_UPDATING = "Updating";
        public const string STATUS_READY = "Ready";
        public const string STATUS_TERMINATING = "Terminating";
        public const string STATUS_TERMINATED = "Terminated";

        public const string HEALTH_RED = "Red";
        public const string HEALTH_YELLOW = "Yellow";
        public const string HEALTH_GREEN = "Green";
        public const string HEALTH_GREY = "Grey";

        public const string VALIDATION_WARNING = "warning";
        public const string VALIDATION_ERROR = "error";

        // param keys used when inspecting for existing deployment targets
        public const string DeploymentTargetQueryParam_ApplicationName = "ApplicationName";
        public const string DeploymentTargetQueryParam_EnvironmentName = "EnvironmentName";

        public const string INTERNAL_PROPERTIES_NAMESPACE = "aws:cloudformation:template:parameter";

        public const string ENVIRONMENT_NAMESPACE = "aws:elasticbeanstalk:environment";
        public const string ENVIRONMENTTYPE_OPTION = "EnvironmentType";

        public const string EnvType_SingleInstance = "SingleInstance";
        public const string EnvType_LoadBalanced = "LoadBalanced";
    }
}
