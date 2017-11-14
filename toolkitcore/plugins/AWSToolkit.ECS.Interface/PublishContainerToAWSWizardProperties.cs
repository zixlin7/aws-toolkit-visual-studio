using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.ECS
{
    public static class PublishContainerToAWSWizardProperties
    {
        /// <summary>
        /// Indicates the success or fail (= user closed) status of the wizard. Used
        /// because the 'review' page of the wizard does the actual upload and on
        /// success, if the auto-close-wizard option is set, invokes CancelRun to
        /// actually shut down the UI which in turn returns 'false' as the output
        /// from the wizard's Run() method.
        /// Type: Boolean.
        /// </summary>
        public static readonly string WizardResult = "wizardResult";

        /// <summary>
        /// The user account selected by the user (if control present) to own the
        /// uploaded function. This can also be used to select an account on entry
        /// to the wizard.
        /// Type: AccountViewModel.
        /// </summary>
        public static readonly string UserAccount = "userAccount";

        /// <summary>
        /// The region to host the function (if control present). This can also be
        /// used to select a region on entry to the wizard.
        /// Type: RegionEndpointsManager.RegionEndpoints.
        /// </summary>
        public static readonly string Region = "region";

        public static readonly string IsWebProject = "isWebProject";

        public static readonly string SelectedProjectFile = "selectedProjectFile";
        public static readonly string SourcePath = "sourcePath";
        public static readonly string SafeProjectName = "safeProjectName";

        public static readonly string DeploymentMode = "deploymentMode";
        public static readonly string Configuration = "configuration";
        public static readonly string DockerRepository = "dockerRepository";
        public static readonly string DockerTag = "dockerTag";

        public static readonly string TaskDefinition = "taskDefinition";
        public static readonly string Container = "container";
        public static readonly string MemoryHardLimit = "memoryHardLimit";
        public static readonly string MemorySoftLimit = "memorySoftLimit";
        public static readonly string PortMappings = "portMappings";
        public static readonly string EnvironmentVariables = "EnvironmentVariables";
        public static readonly string TaskRole = "TaskRole";
        public static readonly string TaskRoleManagedPolicy = "TaskRoleManagedPolicy";

        public static readonly string CreateNewService = "isExistingService";
        public static readonly string Service = "service";
        public static readonly string DesiredCount = "desiredCount";
        public static readonly string MinimumHealthy = "minimumHealthy";
        public static readonly string MaximumPercent = "maximumPercent";

        public static readonly string VpcId = "VpcId";

        public static readonly string ShouldConfigureELB = "ShouldConfigureELB";
        public static readonly string CreateNewIAMRole = "CreateNewIAMRole";
        public static readonly string ServiceIAMRole = "ServiceIAMRole";
        public static readonly string CreateNewLoadBalancer = "CreateNewLoadBalancer";
        public static readonly string LoadBalancer = "LoadBalancer";
        public static readonly string CreateNewListenerPort = "CreateNewListenerPort";
        public static readonly string NewListenerPort = "NewListenerPort";
        public static readonly string ListenerArn = "ListenerArn";
        public static readonly string CreateNewTargetGroup = "CreateNewTargetGroup";
        public static readonly string TargetGroup = "TargetGroup";
        public static readonly string NewPathPattern = "NewPathPattern";
        public static readonly string HealthCheckPath = "HealthCheckPath";

        public static readonly string ExistingCluster = "existingCluster";
        public static readonly string CreateNewCluster = "CreateNewCluster";
        public static readonly string ClusterName = "ClusterName";

        public static readonly string LaunchType = "LaunchType";
        public static readonly string LaunchSubnets = "LaunchSubnets";
        public static readonly string LaunchSecurityGroups = "LaunchSecurityGroups";

        public static readonly string TaskGroup = "TaskGroup";

        public static readonly string PlacementConstraints = "PlacementConstraints";
        public static readonly string PlacementStrategy = "PlacementStrategy";


        public static readonly string ScheduleTaskRuleName = "ScheduleTaskRuleName";
        public static readonly string CloudWatchEventIAMRole = "CloudWatchEventIAMRole";
        public static readonly string CreateCloudWatchEventIAMRole = "CloudWatchEventIAMRole";
        public static readonly string ScheduleTaskRuleTarget = "ScheduleTaskRuleTarget";
        public static readonly string ScheduleExpression = "ScheduleExpression";

        public static readonly string PersistSettingsToConfigFile = "persistSettingsToConfigFile";
    }
}
