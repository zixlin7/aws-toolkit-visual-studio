using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.ElasticBeanstalk
{
    public static class BeanstalkDeploymentWizardProperties
    {
        // for seed data related to incremental deployment, that can be found in the previous
        // deployment's history record so no need for specific seed properties
        public static class SeedData
        {
            /// <summary>
            /// String, optional. If the application has been deployed previously,
            /// holds the name of the environment that was used.
            /// </summary>
            public static readonly string propkey_SeedEnvName = "seedEnvName";
        }

        public static class ApplicationProperties
        {
            /// <summary>
            /// String, description of app to create; only set if create-new-app true
            /// </summary>
            public static readonly string propkey_AppDescription = "appDescription";

            /// <summary>
            /// String, version label to be applied to deployment
            /// </summary>
            public static readonly string propkey_VersionLabel = "versionLabel";

            public static readonly string propkey_EnableXRayDaemon = "enableXRayDaemon";

        }

        public static class DeploymentModeProperties
        {
            /// <summary>
            /// Bool, optional. If true, perform deployment via Git pushes
            /// </summary>
            public static readonly string propkey_IncrementalDeployment = "incrementalDeployment";

            /// <summary>
            /// String, optional. If set and incremental deployment is signalled, the folder location of a 
            /// Git repository to which the deployment artifacts should be committed prior to deployment push. 
            /// The Git repository will be created if needed.
            /// </summary>
            public static readonly string propkey_IncrementalPushRepositoryLocation = "incrementalPushRepositoryLocation";
        }

        public static class EnvironmentProperties
        {
            /// <summary>
            /// Boolean, set if the user checks the 'create new environment' option. If not set,
            /// use-existing should be assumed.
            /// </summary>
            public static readonly string propkey_CreateNewEnv = "createNewEnv";

            /// <summary>
            /// String, name of env to create or reuse
            /// </summary>
            public static readonly string propkey_EnvName = "envName";

            /// <summary>
            /// String, description of env to create; only set if create-new-env true
            /// </summary>
            public static readonly string propkey_EnvDescription = "envDescription";

            /// <summary>
            /// String, custom cname to apply for the environment (not relevant for single-instance)
            /// </summary>
            public static readonly string propkey_CName = "cName";

            /// <summary>
            /// String, the selected environment type for a new environment. Currently supports
            /// 'SingleInstance' or 'LoadBalanced'. If empty or not set, 'LoadBalanced' is assumed
            /// by the service.
            /// </summary>
            public static readonly string propkey_EnvType = "envType";

            /// <summary>
            /// Boolean which if true tells the wizard to show the rolling deployment page.
            /// </summary>
            public static readonly string propkey_EnableRollingDeployments = "enableRollingDeployments";
        }

        public static class AWSOptionsProperties
        {
            /// <summary>
            /// [Currently unused], designation of the region to deploy to
            /// </summary>
            public static readonly string propkey_Region = "region";

            /// <summary>
            /// String, name of the container
            /// </summary>
            public static readonly string propkey_SolutionStack = "solutionStack";

            /// <summary>
            /// InstanceProfile instance describing the role to launch instances with
            /// </summary>
            public static readonly string propkey_InstanceProfileName = "instanceProfile";
            public static readonly string propkey_PolicyTemplates = "policyTemplates";

            public static readonly string propkey_LaunchIntoVPC = "launch-into-vpc";
            public static readonly string propkey_VPCId = "vpc-id";
            public static readonly string propkey_InstanceSubnet = "instance-subnet";
            public static readonly string propkey_ELBSubnet = "elb-subnet";
            public static readonly string propkey_ELBScheme = "elb-scheme";
            public static readonly string propkey_VPCSecurityGroup = "vpc-security-group";
            public static readonly string propkey_ServiceRoleName = "serviceRole";
        }

        public static class AppOptionsProperties
        {
            /// <summary>
            /// Bool, optional. Set during redeployment if the wizard detects any changes to the
            /// application options (app pool, healthcheck, notification email (beanstalk only),
            /// PARAM1...5 or credentials.
            /// </summary>
            public static readonly string propkey_AppOptionsUpdated = "appOptionsUpdated";

            /// <summary>
            /// [Optional], String, email address for notification emails
            /// </summary>
            public static readonly string propkey_NotificationEmail = "notificationEmail";

            /// <summary>
            /// Bool, indicates if xray is available in the region the app is being deployed to
            /// and therefore we can offer to enable.
            /// </summary>
            public static readonly string propkey_XRayAvailable = "xrayAvailableInRegion";

        }

        public static class DatabaseOptions
        {
            /// <summary>
            /// List of string, optional. Contains a list of one or more RDS security group 
            /// names that the EC2 security group for the Beanstalk instance will be added to.
            /// </summary>
            public static readonly string propkey_RDSSecurityGroups = "rdsSecurityGroups";

            /// <summary>
            /// List of string, optional. Contains a list of one or more EC2-VPC security
            /// group ids that the EC2 security group for the Beanstalk instance will be added to.
            /// </summary>
            public static readonly string propkey_VPCSecurityGroups = "vpcSecurityGroups";

            /// <summary>
            /// Used to carry details of the db instance ports we need to use to wire up access
            /// to a vpc-based db instance
            /// </summary>
            public static readonly string propkey_VPCGroupsAndDBInstances = "vpcGroupsAndDbInstances";
        }


        public static class RollingDeployments
        {
            public static readonly string propKey_AppBatchSize = "appBatchType";

            public static readonly string propKey_AppBatchType = "appBatchSize";


            public static readonly string propKey_EnableConfigRollingDeployment = "enableConfigRollingDeployment";

            public static readonly string propKey_ConfigMaximumBatchSize = "configBatchSize";

            public static readonly string propKey_ConfigMinInstanceInServices = "configMinInstanceInService";
        }
    }
}
