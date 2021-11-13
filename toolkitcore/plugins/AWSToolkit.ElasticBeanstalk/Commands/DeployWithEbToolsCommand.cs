using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.Common.DotNetCli.Tools;
using Amazon.ElasticBeanstalk.Tools.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;
using Amazon.AWSToolkit.Regions;
using AWSDeployment;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Commands
{
    public class DeployWithEbToolsCommand : BaseBeanstalkDeployCommand
    {
        private const string OptionCustomImageId = "aws:autoscaling:launchconfiguration,ImageId";

        public const string OptionSecurityGroups = "aws:autoscaling:launchconfiguration,SecurityGroups";
        public const string OptionInstanceSubnets = "aws:ec2:vpc,Subnets";
        public const string OptionELBSubnets = "aws:ec2:vpc,ELBSubnets";
        public const string OptionELBScheme = "aws:ec2:vpc,ELBScheme";

        private const string OptionRollingUpdateMaxBatchSize =
            "aws:autoscaling:updatepolicy:rollingupdate,MaxBatchSize";

        private const string OptionRollingUpdateMinInstancesInService =
            "aws:autoscaling:updatepolicy:rollingupdate,MinInstancesInService";

        private const string OptionRollingBatchType = "aws:elasticbeanstalk:command,BatchType";
        private const string OptionRollingBatchSize = "aws:elasticbeanstalk:command,BatchSize";

        string _projectLocation;

        public DeployWithEbToolsCommand(string projectLocation, IDictionary<string, object> deploymentProperties)
            : this(projectLocation, deploymentProperties, null)
        {
        }

        public DeployWithEbToolsCommand(
            string projectLocation,
            IDictionary<string, object> deploymentProperties,
            DeploymentControllerObserver observer)
            : base(deploymentProperties, observer)
        {
            this._projectLocation = projectLocation;
        }

#if DEBUG
        /// <summary>
        /// Test method to save the settings as a JSON document used to help construct unit tests.
        /// Should not be called real builds of the toolkits.
        /// </summary>
        private void WriteDeploymentPropertiesToJson()
        {
            var skipKeys = new HashSet<string>
                {"previousDeployments", "existingAppDeploymentsInRegion", "serviceReviewPanels"};
            var serializedProps = new Dictionary<string, object>();
            foreach (var kvp in this.DeploymentProperties)
            {
                if (skipKeys.Contains(kvp.Key))
                {
                    continue;
                }

                var type = kvp.Value.GetType();
                if (type.IsPrimitive || type == typeof(string))
                {
                    serializedProps[kvp.Key] = kvp.Value;
                }
            }

            var json = ThirdParty.Json.LitJson.JsonMapper.ToJson(serializedProps);
            System.IO.File.WriteAllText(@"c:\temp\deployment.json", json);
        }
#endif

        public override void Execute()
        {
            var statusLogger = new DeployToolLogger();
            var success = false;
            var deployMetric = new BeanstalkDeploy()
            {
                Name = getValue<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack),
                Framework = getValue<string>(DeploymentWizardProperties.AppOptions.propkey_TargetRuntime),
                EnhancedHealthEnabled = false,
                XrayEnabled = false,
            };

            var command = new DeployEnvironmentCommand(statusLogger, this._projectLocation, new string[0]);
            command.DisableInteractive = true;
            command.DeployEnvironmentOptions.WaitForUpdate = false;
            var region = CommonWizardProperties.AccountSelection.GetSelectedRegion(DeploymentProperties);
            command.Region = region.Id;
            deployMetric.AwsRegion = command.Region;

            command.IAMClient =
                this.Account.CreateServiceClient<Amazon.IdentityManagement.AmazonIdentityManagementServiceClient>(
                    region);
            command.S3Client =
                this.Account.CreateServiceClient<Amazon.S3.AmazonS3Client>(
                    region);
            command.EBClient =
                this.Account.CreateServiceClient<Amazon.ElasticBeanstalk.AmazonElasticBeanstalkClient>(
                    region);


            try
            {
                SetPropertiesForDeployCommand(deployMetric, command, region);

                EnsureRolesExist(command, region);

                success = command.ExecuteAsync().GetAwaiter().GetResult();
                deployMetric.Result = success ? Result.Succeeded : Result.Failed;
                if (!success && command.LastToolsException != null)
                {
                    throw command.LastToolsException;
                }
            }
            catch (Exception e)
            {
                deployMetric.Result = Result.Failed;
                string errMsg = string.Format("Error publishing application: {0}", e.Message);
                statusLogger.WriteLine(errMsg);
                ToolkitFactory.Instance.ShellProvider.ShowError("Publish Error", errMsg);
            }
            finally
            {
                ReportFinalStatus(deployMetric, success, command);
            }
        }

        /// <summary>
        /// Creates the specified Role and ServiceRole if necessary
        /// </summary>
        private void EnsureRolesExist(DeployEnvironmentCommand command,
            ToolkitRegion region)
        {
            if (!string.IsNullOrWhiteSpace(command.DeployEnvironmentOptions.InstanceProfile))
            {
                BeanstalkDeploymentEngine.ConfigureRoleAndProfile(
                    command.IAMClient,
                    command.DeployEnvironmentOptions.InstanceProfile,
                    region.Id,
                    this.Observer
                );
            }

            if (!string.IsNullOrWhiteSpace(command.DeployEnvironmentOptions.ServiceRole))
            {
                BeanstalkDeploymentEngine.ConfigureServiceRole(
                    command.IAMClient,
                    command.DeployEnvironmentOptions.ServiceRole,
                    region.Id,
                    this.Observer
                );
            }
        }

        private void ReportFinalStatus(BeanstalkDeploy deployMetric, bool success, DeployEnvironmentCommand command)
        {
            try
            {
                ToolkitFactory.Instance.TelemetryLogger.RecordBeanstalkDeploy(deployMetric);
                ToolkitFactory.Instance.ShellProvider.UpdateStatus(string.Empty);
                if (success)
                {
                    Observer.Info("Publish to AWS Elastic Beanstalk environment '{0}' completed successfully",
                        command.DeployEnvironmentOptions.Environment);

                    DeploymentTaskNotifier notifier = new DeploymentTaskNotifier();
                    notifier.BeanstalkClient = command.EBClient;
                    notifier.ApplicationName = command.DeployEnvironmentOptions.Application;
                    notifier.EnvironmentName = command.DeployEnvironmentOptions.Environment;
                    TaskWatcher.WatchAndNotify(TaskWatcher.DefaultPollInterval, notifier);

                    SelectNewTreeItems(command.DeployEnvironmentOptions.Application,
                        command.DeployEnvironmentOptions.Environment);
                }
                else
                    Observer.Info("Publish to AWS Elastic Beanstalk environment '{0}' did not complete successfully",
                        command.DeployEnvironmentOptions.Environment);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error selecting new tree items", e);
            }
        }

        public void SetPropertiesForDeployCommand(
            BeanstalkDeploy deployMetric,
            DeployEnvironmentCommand command,
            ToolkitRegion region)
        {
            SetPropertiesForDeployCommand(deployMetric, command, region, GetDefaultVPCSubnet);
        }

        public void SetPropertiesForDeployCommand(
            BeanstalkDeploy deployMetric,
            DeployEnvironmentCommand command,
            ToolkitRegion region,
            GetDefaultVpcSubnetFunc fnGetDefaultVpcSubnet)
        {
            var redeployMode = getValue<bool>(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy);
            deployMetric.InitialDeploy = !redeployMode;

            command.PersistConfigFile =
                getValue<bool>(DeploymentWizardProperties.ReviewProperties.propkey_SaveBeanstalkTools);

            command.DeployEnvironmentOptions.TargetFramework =
                getValue<string>(DeploymentWizardProperties.AppOptions.propkey_TargetRuntime);
            command.DeployEnvironmentOptions.Configuration =
                getValue<string>(DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration);

            // If there is a platform component like "Any CPU" strip it off. EbTools does not currently support setting platform setting.
            if ((command.DeployEnvironmentOptions.Configuration?.Contains('|')).GetValueOrDefault())
            {
                command.DeployEnvironmentOptions.Configuration =
                    command.DeployEnvironmentOptions.Configuration.Substring(0,
                        command.DeployEnvironmentOptions.Configuration.IndexOf('|'));
            }

            command.DeployEnvironmentOptions.Application =
                getValue<string>(DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName);
            command.DeployEnvironmentOptions.VersionLabel =
                getValue<string>(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel);
            command.DeployEnvironmentOptions.Environment =
                getValue<string>(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName);
            command.DeployEnvironmentOptions.ProxyServer = getValue<string>(BeanstalkDeploymentWizardProperties
                .EnvironmentProperties.propkey_ReverseProxyMode);

            var isLoadBalancedDeployment =
                string.Equals(
                    getValue<string>(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType),
                    BeanstalkConstants.EnvType_LoadBalanced, StringComparison.OrdinalIgnoreCase);

            if (!redeployMode)
            {
                command.DeployEnvironmentOptions.CNamePrefix =
                    getValue<string>(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CName);
                command.DeployEnvironmentOptions.EnvironmentType =
                    getValue<string>(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType);
                command.DeployEnvironmentOptions.SolutionStack = getValue<string>(BeanstalkDeploymentWizardProperties
                    .AWSOptionsProperties.propkey_SolutionStack);
                command.DeployEnvironmentOptions.InstanceType =
                    getValue<string>(DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeID);
                command.DeployEnvironmentOptions.LoadBalancerType = getValue<string>(BeanstalkDeploymentWizardProperties
                    .EnvironmentProperties.propkey_LoadBalancerType);

                // The wizard treats classic loadbalancer as an empty string by EbTools it must be set as "classic"
                if (string.IsNullOrEmpty(command.DeployEnvironmentOptions.LoadBalancerType) && isLoadBalancedDeployment)
                {
                    command.DeployEnvironmentOptions.LoadBalancerType =
                        Amazon.ElasticBeanstalk.Tools.EBConstants.LOADBALANCER_TYPE_CLASSIC;
                }
            }


            if (isLoadBalancedDeployment &&
                !string.IsNullOrEmpty(getValue<string>(DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl)))
            {
                command.DeployEnvironmentOptions.HealthCheckUrl =
                    getValue<string>(DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl);
            }

            if (getValue<object>(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon) is
                bool)
            {
                command.DeployEnvironmentOptions.EnableXRay = getValue<bool>(BeanstalkDeploymentWizardProperties
                    .ApplicationProperties.propkey_EnableXRayDaemon);

                if (command.DeployEnvironmentOptions.EnableXRay.GetValueOrDefault())
                {
                    deployMetric.XrayEnabled = true;
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.XRayEnabled, "Beanstalk");
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
                }
            }

            if (getValue<object>(BeanstalkDeploymentWizardProperties.ApplicationProperties
                .propkey_EnableEnhancedHealth) is bool)
            {
                var enableEnhancedHealth = getValue<bool>(BeanstalkDeploymentWizardProperties.ApplicationProperties
                    .propkey_EnableEnhancedHealth);
                command.DeployEnvironmentOptions.EnhancedHealthType = enableEnhancedHealth
                    ? Amazon.ElasticBeanstalk.Tools.EBConstants.ENHANCED_HEALTH_TYPE_ENHANCED
                    : Amazon.ElasticBeanstalk.Tools.EBConstants.ENHANCED_HEALTH_TYPE_BASIC;

                if (enableEnhancedHealth)
                {
                    deployMetric.EnhancedHealthEnabled = true;
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.BeanstalkEnhancedHealth, "true");
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
                }
            }

            if (IsWindows())
            {
                AddIISPropertiesTo(command);
            }

            command.DeployEnvironmentOptions.SelfContained =
                getValue<bool>(DeploymentWizardProperties.AppOptions.propkey_BuildSelfContainedBundle);

            if (!redeployMode)
            {
                command.DeployEnvironmentOptions.EC2KeyPair =
                    getValue<string>(DeploymentWizardProperties.AWSOptions.propkey_KeyPairName);
                if (getValue<bool>(DeploymentWizardProperties.AWSOptions.propkey_CreateKeyPair))
                {
                    CreateKeyPair(Account, region, command.DeployEnvironmentOptions.EC2KeyPair);
                }


                command.DeployEnvironmentOptions.InstanceProfile = ConfigureIAMRole(Account, region);
                command.DeployEnvironmentOptions.ServiceRole = getValue<string>(BeanstalkDeploymentWizardProperties
                    .AWSOptionsProperties.propkey_ServiceRoleName);
            }

            command.DeployEnvironmentOptions.AdditionalOptions = BuildAdditionalOptionsCollection(region,
                redeployMode, isLoadBalancedDeployment, fnGetDefaultVpcSubnet);

            var rdsSecurityGroups =
                getValue<List<string>>(BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_RDSSecurityGroups);
            var vpcSecurityGroups =
                getValue<List<string>>(BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_VPCSecurityGroups);
            var vpcGroupsAndReferencingDBInstances =
                getValue<Dictionary<string, List<int>>>(BeanstalkDeploymentWizardProperties.DatabaseOptions
                    .propkey_VPCGroupsAndDBInstances);


            if ((rdsSecurityGroups != null && rdsSecurityGroups.Any()) ||
                (vpcSecurityGroups != null && vpcSecurityGroups.Any()))
            {
                var ec2Client =
                    this.Account.CreateServiceClient<Amazon.EC2.AmazonEC2Client>(region);
                var rdsClient =
                    this.Account.CreateServiceClient<Amazon.RDS.AmazonRDSClient>(region);

                GetVpcDetails(out var launchIntoVpc, out var vpcId);

                var securityGroup = AWSDeployment.BeanstalkDeploymentEngine.SetupEC2GroupForRDS(this.Observer,
                    ec2Client, rdsClient,
                    command.DeployEnvironmentOptions.Environment, vpcId, launchIntoVpc,
                    rdsSecurityGroups, vpcSecurityGroups, vpcGroupsAndReferencingDBInstances);

                if (command.DeployEnvironmentOptions.AdditionalOptions == null)
                {
                    command.DeployEnvironmentOptions.AdditionalOptions = new Dictionary<string, string>();
                }

                string existingSecurityGroupValue;
                if (command.DeployEnvironmentOptions.AdditionalOptions.TryGetValue(OptionSecurityGroups,
                    out existingSecurityGroupValue))
                {
                    existingSecurityGroupValue += "," + securityGroup;
                }
                else
                {
                    existingSecurityGroupValue = securityGroup;
                }

                command.DeployEnvironmentOptions.AdditionalOptions[OptionSecurityGroups] = existingSecurityGroupValue;
            }
        }

        private bool IsWindows()
        {
            return !getValue<bool>(BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propKey_IsLinuxSolutionStack);
        }

        private void AddIISPropertiesTo(DeployEnvironmentCommand command)
        {
            var iisPath = new IisPath(getValue<string>(DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath));
            command.DeployEnvironmentOptions.IISWebSite = iisPath.WebSite;
            command.DeployEnvironmentOptions.UrlPath = iisPath.AppPath;
        }

        public Dictionary<string, string> BuildAdditionalOptionsCollection(
            ToolkitRegion region, bool redeployMode, bool isLoadBalanced,
            GetDefaultVpcSubnetFunc fnGetDefaultVpcSubnet)
        {
            var options = new Dictionary<string, string>();

            Action<string, string> addIfValueExists = (beanstalkName, optionName) =>
            {
                if (!string.IsNullOrEmpty(getValue<string>(optionName)))
                {
                    options[beanstalkName] = getValue<string>(optionName);
                }
            };

            if (!redeployMode)
            {
                addIfValueExists(OptionCustomImageId, DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID);

                // VPC Settings.
                GetVpcDetails(out var launchIntoVpc, out var vpcId);

                if (launchIntoVpc)
                {
                    addIfValueExists(OptionSecurityGroups,
                        BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_VPCSecurityGroup);
                    addIfValueExists(OptionInstanceSubnets,
                        BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceSubnet);

                    if (isLoadBalanced)
                    {
                        addIfValueExists(OptionELBSubnets,
                            BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ELBSubnet);
                        addIfValueExists(OptionELBScheme,
                            BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ELBScheme);

                        if (string.IsNullOrEmpty(getValue<string>(BeanstalkDeploymentWizardProperties
                            .AWSOptionsProperties.propkey_ELBSubnet)))
                        {
                            options[OptionELBSubnets] = fnGetDefaultVpcSubnet(Account, region);
                        }
                    }
                }

                // Rolling Deployment Settings
                if (getValue<bool>(BeanstalkDeploymentWizardProperties.RollingDeployments
                    .propKey_EnableConfigRollingDeployment))
                {
                    options[OptionRollingUpdateMaxBatchSize] = getValue<int>(BeanstalkDeploymentWizardProperties
                        .RollingDeployments.propKey_ConfigMaximumBatchSize).ToString();
                    options[OptionRollingUpdateMinInstancesInService] =
                        getValue<int>(BeanstalkDeploymentWizardProperties.RollingDeployments
                            .propKey_ConfigMinInstanceInServices).ToString();
                }

                if (DeploymentProperties.ContainsKey(BeanstalkDeploymentWizardProperties.RollingDeployments
                    .propKey_AppBatchType))
                {
                    addIfValueExists(OptionRollingBatchType,
                        BeanstalkDeploymentWizardProperties.RollingDeployments.propKey_AppBatchType);

                    var batchSize =
                        getValue<int>(BeanstalkDeploymentWizardProperties.RollingDeployments.propKey_AppBatchSize);
                    if (batchSize > 0)
                    {
                        options[OptionRollingBatchSize] = batchSize.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

            return options;
        }


        internal class DeployToolLogger : IToolLogger
        {
            public void WriteLine(string message)
            {
                ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(message, true);
                ToolkitFactory.Instance.ShellProvider.UpdateStatus(message);
            }

            public void WriteLine(string message, params object[] args)
            {
                WriteLine(string.Format(message, args));
            }
        }
    }
}
