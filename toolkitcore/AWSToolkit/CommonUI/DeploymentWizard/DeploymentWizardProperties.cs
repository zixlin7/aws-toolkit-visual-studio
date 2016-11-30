namespace Amazon.AWSToolkit.CommonUI.DeploymentWizard
{
    /// <summary>
    /// The collection of page groups in which pages can be registered in the
    /// new deployment wizard.
    /// </summary>
    public static class DeploymentWizardPageGroups
    {
        public static string AppTargetGroup = "Application";
        public static string AWSOptionsGroup = "AWS Options";
        public static string PermissionsGroup = "Permissions";
        public static string AppOptionsGroup = "Options";
        public static string ReviewGroup = "Review";

        public static string[] DeploymentPageGroups =
        {
            AppTargetGroup,
            AWSOptionsGroup,
            PermissionsGroup,
            AppOptionsGroup,
            ReviewGroup
        };
    }

    /// <summary>
    /// Common x-plugin properties for the deployment wizard (including the legacy version)
    /// </summary>
    public static class DeploymentWizardProperties
    {
        /// <summary>
        /// Used with propkey_ProjectType, this indicates a traditional asp.net project
        /// </summary>
        public static readonly string StandardWebProject = "standard";
        /// <summary>
        /// Used with propkey_ProjectType, this indicates a new CoreCLR based web project
        /// </summary>
        public static readonly string NetCoreWebProject = "netcore";

        public static class SeedData
        {
            /// <summary>
            /// String, optional. For new deployments, contains suggested name to 
            /// give to the deployed application stack based on project name or data 
            /// from last deployment persisted with the project.
            /// </summary>
            public static readonly string propkey_SeedName = "seedName";

            /// <summary>
            /// String => object dictionary, optional. Contains the persisted previous
            /// deployments keyed to the service name. The value is an instance of
            /// deployment history objects relevant to the particular service.
            /// </summary>
            public static readonly string propkey_PreviousDeployments = "previousDeployments";

            /// <summary>
            /// String, optional. If previous deployment data is available, contains the name
            /// of the service that was last used to deploy with.
            /// </summary>
            public static readonly string propkey_LastServiceDeployedTo = "lastServiceDeployedTo";

            /// <summary>
            /// String, optional. System name of the region the app was deployed to.
            /// </summary>
            public static readonly string propkey_LastRegionDeployedTo = "lastRegionDeployedTo";

            /// <summary>
            /// Guid, set if redeploying app, the toolkit identifier guid of the account view
            /// model instance that was used the last time the app was deployed
            /// </summary>
            public static readonly string propkey_SeedAccountGuid = "seedAccountGuid";

            /// <summary>
            /// String, the id of a custom ami to use instead of the one declared in whatever
            /// template the user selects
            /// </summary>
            public static readonly string propkey_CustomAMIID = "customAMIID";

            /// <summary>
            /// Boolean, optional. If set true the option to start the legacy wizard will
            /// be hidden (used for application version redeployments) and other control
            /// disabled so all the user can do is send an existing archive to a new
            /// environment or an existing one.
            /// </summary>
            public static readonly string propkey_RedeployingAppVersion = "redeployingAppVersion";

            /// <summary>
            /// String, optional. Suggested version label for the deployment, based
            /// on data persisted with the project on last-run or a default initial
            /// value (current date/time).
            /// </summary>
            public static readonly string propkey_SeedVersionLabel = "seedVersionLabel";

            /// <summary>
            /// HashSet<string>, contains one or more service owner names of available deployment services.
            /// Can be used to filter deployment templates to exclude those that do not have the
            /// relevant service available.
            /// </summary>
            public static readonly string propkey_AvailableServiceOwners = "availableServiceOwners";

            /// <summary>
            /// String. The unique guid assigned to the project by Visual Studio.
            /// </summary>
            public static readonly string propkey_VSProjectGuid = "vsProjectGuid";

            /// <summary>
            /// Whether the project is targetting standard .Net framework or .Net Core
            /// </summary>
            public static readonly string propkey_ProjectType = "vsProjectType";

            /// <summary>
            /// Optional, Dictionary<string, string>. Set of template property overrides 
            /// used with the template on the last deployment or cost estimation
            /// </summary>
            public static readonly string propkey_TemplateProperties = "templateProperties";

            /// <summary>
            /// Set if the user requested that we launch and use the legacy deployment wizard.
            /// </summary>
            public static readonly string propkey_LegacyDeploymentMode = "legacyDeploymentMode";

            /// <summary>
            /// Map of solution configurations and the project configuration that is contained
            /// in the configuration context. Automation builds use the solution configuration
            /// name, msbuild builds use the project configuration name.
            /// </summary>
            public static readonly string propkey_ProjectBuildConfigurations = "projectBuildConfigurations";

            /// <summary>
            /// The name of the project build configuration currently active in the IDE; the wizard
            /// will build this configuration by default
            /// </summary>
            public static readonly string propkey_ActiveBuildConfiguration = "activeBuildConfiguration";

            /// <summary>
            /// Contains the DeployIisAppPath setting from the user project, if set.
            /// </summary>
            public static readonly string propkey_DeployIisAppPath = "deployIisAppPath";

            /// <summary>
            /// Indicates if the user account and/or selected region is locked to vpc only
            /// usage 
            /// </summary>
            public static readonly string propkey_VpcOnlyMode = "vpcOnlyMode";

            /// <summary>
            /// .NET framework (coreclr) or runtime (traditional) that can be used with the project.
            /// Dictionary of UI text to the code that we send to our back-end deployment code to
            /// set the apppool etc on the deployment host.
            /// </summary>
            public static readonly string propkey_ProjectFrameworks = "projectFrameworks";
        }

        public static class DeploymentTemplate
        {
            /// <summary>
            /// String, holds the name of the service that owns the selected template, ie the service
            /// to which we will deploy using the template
            /// </summary>
            public static readonly string propkey_TemplateServiceOwner = "templateServiceOwner";

            /// <summary>
            /// Bool. Set true if user chose to deploy app to existing CloudFormation/Beanstalk
            /// instance
            /// </summary>
            public static readonly string propkey_Redeploy = "redeploy";

            /// <summary>
            /// Optional, bool. Set true to redeploy a prior version of an application (ie the
            /// deployment package is already present in S3).
            /// Note: currently supported for Beanstalk deployments only.
            /// </summary>
            public static readonly string propkey_RedeployVersion = "redeployVersion";

            /// <summary>
            /// DeploymentTemplateWrapperBase-derived instance for the selected template if not redeploying
            /// </summary>
            public static readonly string propkey_SelectedTemplate = "selectedTemplate";

            /// <summary>
            /// Name of the template used when uploading to S3 instance for the selected template if not redeploying
            /// </summary>
            public static readonly string propkey_SelectedTemplateName = "selectedTemplateName";

            /// <summary>
            /// String, stack name for CloudFormation deployments, application name for Beanstalk
            /// </summary>
            public static readonly string propkey_DeploymentName = "name";

            /// <summary>
            /// ExistingDeployment instance, optional. If in redeployment mode, holds the 
            /// CloudFormation Stack instance or Beanstalk application name selected to redeploy to.
            /// Null for new deployments.
            /// </summary>
            public static readonly string propkey_RedeploymentInstance = "redeploymentInstance";

            /// <summary>
            /// Used by the new wizard, this contains a collection of DeployedApplicationModel instances 
            /// for a customer in the selected region, as queried by the deployment wizard start page. Passing
            /// this collection to downstream pages saves us from spinning up new workers to recover information 
            /// already queried by the start page.
            /// </summary>
            public static readonly string propkey_ExistingAppDeploymentsInRegion = "existingAppDeploymentsInRegion";
        }

        public static class AWSOptions
        {
            /// <summary>
            /// String, optional. ID of the custom ami to use instead of the default
            /// in the Beanstalk solution stack or CloudFormation templates.
            /// </summary>
            public static readonly string propkey_CustomAMIID = "customAMIID";

            /// <summary>
            /// String, instance type id (eg t1.micro)
            /// </summary>
            public static readonly string propkey_InstanceTypeID = "instanceTypeID";

            /// <summary>
            /// String, descriptive name of the selected instance type id; mainly used for review/UI
            /// purposes
            /// </summary>
            public static readonly string propkey_InstanceTypeName = "instanceTypeName";

            /// <summary>
            /// String, name of the key pair to use or create during deployment.
            /// Empty string if the user did not specify a key name.
            /// </summary>
            public static readonly string propkey_KeyPairName = "keyPairName";

            /// <summary>
            /// Boolean, if set create a keypair with the name held by propkey_KeyPair
            /// otherwise assume a keypair with that name already exists.
            /// Only present if propkey_KeyPair does not evaluate to an empty string.
            /// </summary>
            public static readonly string propkey_CreateKeyPair = "createKeyPair";

            /// <summary>
            /// Bool, optional. If true a rule to open port 80 should be added to the specified
            /// security group during deployment.
            /// </summary>
            public static readonly string propkey_AutoOpenPort80 = "autoOpenPort80";

            /// <summary>
            /// String, name of the selected security group
            /// </summary>
            public static readonly string propkey_SecurityGroupName = "securityGroupName";

            /// <summary>
            /// String, the id of any default vpc we discovered during the wizard for the
            /// selected user account and region
            /// </summary>
            public static readonly string propkey_DefaultVpcId = "defaultVpcId";
        }

        public static class AppOptions
        {
            /// <summary>
            /// String, optional, major.minor version label of the target runtime of the application.
            /// </summary>
            public static readonly string propkey_TargetRuntime = "targetRuntime";

            /// <summary>
            /// Boolean, optional - passed into wizard when invoked from vs2008 environment where the v4
            /// runtime is not supported by any project type
            /// </summary>
            public static readonly string propkey_ShowV2RuntimeOnly = "showV2RuntimeOnly";

            /// <summary>
            /// Bool, whether to enable 32 bit application support in destination app pool
            /// </summary>
            public static readonly string propkey_Enable32BitApplications = "enable32BitApps";

            /// <summary>
            /// [Optional], String, url for health check probing
            /// </summary>
            public static readonly string propkey_HealthCheckUrl = "healthCheckUrl";

            /// <summary>
            /// The name of the project build configuration the user selected to build for deployment; this
            /// may or may not be the same as the IDEs selected build configuration
            /// </summary>
            public static readonly string propkey_SelectedBuildConfiguration = "selectedBuildConfiguration";

            /// <summary>
            /// Contains the iis app we will deploy to. This can be forced by the user via a project
            /// setting or is formed by default in the application page.
            /// </summary>
            public static readonly string propkey_DeployIisAppPath = "deployIisAppPath";

            /// <summary>
            /// Contains the key value pair appsettings that will be set in the web.config
            /// of the deployed application.  The legacy PARAM1-5, AccessKey and SecretKey
            /// settings will also be contained in here.
            /// </summary>
            public static readonly string propkey_EnvAppSettings = "envAppSettings";

            public static readonly string propkey_EnvAccessKey = "envAccessKeyID";
            public static readonly string propkey_EnvSecretKey = "envSecretKey";
        }

        public static class ReviewProperties
        {
            /// <summary>
            /// 'Private' property, List<ServiceReviewPanelInfo> containing header and text of the
            /// panels the deployment service wants posted into the review page.
            /// </summary>
            public static readonly string propkey_ServiceReviewPanels = "serviceReviewPanels";

            /// <summary>
            /// Boolean, if true the host environment should open a status window onto
            /// the new deployment when the wizard closes
            /// </summary>
            public static readonly string propkey_LaunchStatusOnClose = "launchStatusOnClose";

            /// <summary>
            /// Boolean, if true a config file corresponding to the environment will
            /// be saved
            /// </summary>
            public static readonly string propkey_ConfigFileDestination = "configFileDestination";
        }
    }
}
