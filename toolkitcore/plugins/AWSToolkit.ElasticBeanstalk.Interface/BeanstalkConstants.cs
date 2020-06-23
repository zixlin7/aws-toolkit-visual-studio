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

        public const string DEFAULT_INSTANCE_TYPE = "t3a.medium";

        public static class SolutionStackNames
        {
            public static class Prefixes
            {
                public const string AmazonLinux2_64Bit = "64bit Amazon Linux 2";
                public const string WindowsServer2019_64Bit = "64bit Windows Server 2019";

                public static class WithVersionDecorator
                {
                    public static readonly string AmazonLinux2_64Bit = $"{Prefixes.AmazonLinux2_64Bit} v";
                    public static readonly string WindowsServer2019_64Bit = $"{Prefixes.WindowsServer2019_64Bit} v";
                }
            }

            public static class Systems
            {
                public const string AmazonLinux = "Amazon Linux";
                public const string WindowsServer2019Core = "Windows Server Core 2019";
                public const string WindowsServer2019 = "Windows Server 2019";
                public const string WindowsServer2016Core = "Windows Server Core 2016";
                public const string WindowsServer2016 = "Windows Server 2016";
                public const string WindowsServer2012R2Core = "Windows Server Core 2012 R2";
                public const string WindowsServer2012R2 = "Windows Server 2012 R2";
                public const string WindowsServer2012Core = "Windows Server Core 2012";
                public const string WindowsServer2012 = "Windows Server 2012";
                public const string WindowsServer2008 = "Windows Server 2008";
            }
    }
    }
}
