namespace AWSDeployment
{
    internal abstract class DeploymentConstants
    {
        internal const string HOST_MANAGER_VERSION_V1 = "0.1.0.0";
        internal const string HOST_MANAGER_VERSION_V2 = "1.1.0.0";
    }

    public abstract class CommonSectionNames
    {
        public const string GeneralSection = "";
        public const string ContainerSection = "Container";
        public const string EnvironmentSection = "Environment";
        public const string TemplateSection = "Template";
    }

    /// <summary>
    /// Deployment configuration parameters shared across all engines
    /// </summary>
    public abstract class CommonParameters
    {
        public const string GeneralSection_Template = "Template";

        public const string GeneralSection_DeploymentPackage = "DeploymentPackage";
        public const string GeneralSection_Region = "Region";
        public const string GeneralSection_UploadBucket = "UploadBucket";
        public const string GeneralSection_KeyPair = "KeyPair";
        public const string GeneralSection_AWSProfileName = "AWSProfileName";
        public const string GeneralSection_AWSAccessKey = "AWSAccessKey";
        public const string GeneralSection_AWSSecretKey = "AWSSecretKey";

        public const string ContainerSection_Type = "Type";
        public const string ContainerSection_Enable32BitApplications = "Enable32BitApplications";
        public const string ContainerSection_TargetRuntime = "TargetRuntime";

        public const string DefaultIisAppPathFormat = "Default Web Site/";

        // deprecated, use Container.TargetRuntime
        public const string ContainerSection_TargetV2Runtime = "TargetV2Runtime";

        // carry forward misnamed health check param so as to not break existing configs
        public const string ContainerSection_ApplicationHealhcheckPath = "ApplicationHealhcheckPath";
        public const string ContainerSection_ApplicationHealthcheckPath = "ApplicationHealthcheckPath";

        public const string ContainerSection_InstanceType = "InstanceType";
        public const string ContainerSection_AmiID = "AmiID";
    }

    /// <summary>
    /// Adds deployment configuration parameters specific to Elastic Beanstalk
    /// </summary>
    public abstract class BeanstalkParameters
    {
        public const string ApplicationSection = "Application";

        // synonym for ContainerTypeParameter
        public const string GeneralSection_SolutionStack = "SolutionStack";

        public const string GeneralSection_IncrementalPushRepository = "IncrementalPushRepository";

        public const string ApplicationSection_Name = "Name";
        public const string ApplicationSection_Description = "Description";
        public const string ApplicationSection_Version = "Version";

        public const string EnvironmentSection_Name = "Name";
        public const string EnvironmentSection_Description = "Description";
        public const string EnvironmentSection_CNAME = "CNAME";

        public const string ContainerSection_NotificationEmail = "NotificationEmail";

        public const string DefaultRoleName = "aws-elasticbeanstalk-ec2-role";
        public const string DefaultServiceRoleName = "aws-elasticbeanstalk-service-role";

        public const string LogPublishingPolicyName = "Beanstalk-DefaultLogPublishingPolicy";

    }

    /// <summary>
    /// Adds deployment configuration parameters specific to CloudFormation
    /// </summary>
    public abstract class CloudFormationParameters
    {
        public const string SettingsSection = "Settings";

        public const string GeneralSection_StackName = "StackName";

        public const string SettingsSection_SNSTopic = "SNSTopic";
        public const string SettingsSection_CreationTimeout = "CreationTimeout";
        public const string SettingsSection_RollbackOnFailure = "RollbackOnFailure";
    }

    public abstract class RolePolicies
    {
        public static class ServiceRoleArns
        {
            public const string AWSElasticBeanstalkManagedUpdatesCustomerRolePolicy =
                "arn:aws:iam::aws:policy/AWSElasticBeanstalkManagedUpdatesCustomerRolePolicy";

            public const string AWSElasticBeanstalkEnhancedHealth =
                "arn:aws:iam::aws:policy/service-role/AWSElasticBeanstalkEnhancedHealth";
        }
    }
}
