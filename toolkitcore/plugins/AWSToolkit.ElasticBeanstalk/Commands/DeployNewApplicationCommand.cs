using System;
using System.Collections.Generic;
using System.Diagnostics;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.ElasticBeanstalk.View.Components;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using AWSDeployment;
using Amazon.AWSToolkit.Regions;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Commands
{
    public class DeployNewApplicationCommand : BaseBeanstalkDeployCommand
    {
        private readonly BaseMetricSource _deployMetricSource;
        private readonly ToolkitContext _toolkitContext;

        public BeanstalkDeploymentEngine Deployment { get; protected set; }

        public DeployNewApplicationCommand(string deploymentPackage, IDictionary<string, object> deploymentProperties, ToolkitContext toolkitContext)
            : this(deploymentPackage, deploymentProperties, toolkitContext, MetricSources.BeanstalkMetricSource.Project)
        {
        }


        public DeployNewApplicationCommand(string deploymentPackage, IDictionary<string, object> deploymentProperties,
            ToolkitContext toolkitContext, BaseMetricSource deploySource) : base(deploymentProperties)
        {
            _deployMetricSource = deploySource;
            _toolkitContext = toolkitContext;

            Deployment
                = DeploymentEngineFactory.CreateEngine(DeploymentEngineFactory.ElasticBeanstalkServiceName,
                        toolkitContext)
                    as BeanstalkDeploymentEngine;

            var region = CommonWizardProperties.AccountSelection.GetSelectedRegion(DeploymentProperties);
            var credentials = _toolkitContext.CredentialManager.GetAwsCredentials(Account.Identifier, region);

            Deployment.AWSProfileName = Account.Identifier.ProfileName;
            Deployment.Credentials = credentials;
            Deployment.Observer = Observer;
            Deployment.DeploymentPackage = deploymentPackage;
            Deployment.Region = region?.Id;
        }

        public override void Execute()
        {
            if (Account == null)
                return;

            bool success = false;
            var duration = new Stopwatch();
            duration.Start();

            var deployMetric = new BeanstalkDeploy()
            {
                Name = getValue<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack),
                Framework = getValue<string>(DeploymentWizardProperties.AppOptions.propkey_TargetRuntime),
                EnhancedHealthEnabled = false,
                XrayEnabled = false,
                InitialDeploy = !getValue<bool>(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy),
                AwsRegion = Deployment.Region ?? MetadataValue.Invalid,
                Source = _deployMetricSource.Location,
                ServiceType = _deployMetricSource.Service
            };
            try
            {
                if (string.IsNullOrWhiteSpace(Deployment.Region))
                {
                    throw new Exception("Region was not provided to Beanstalk deployment");
                }

                if (Deployment.Credentials == null)
                {
                    throw new Exception("Credentials were not provided to Beanstalk deployment");
                }

                var region = _toolkitContext.RegionProvider.GetRegion(Deployment.Region);
                _toolkitContext.ConnectionManager.ChangeCredentialProvider(Account.Identifier);

                Deployment.ApplicationName =
                    getValue<string>(DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName);
                Deployment.ApplicationDescription = getValue<string>(BeanstalkDeploymentWizardProperties
                    .ApplicationProperties.propkey_AppDescription);
                Deployment.VersionLabel =
                    getValue<string>(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel);

                if (getValue<bool>(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
                {
                    if (getValue<bool>(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeployVersion))
                        Deployment.DeploymentMode = DeploymentEngineBase.DeploymentModes.RedeployPriorVersion;
                    else
                        Deployment.DeploymentMode = DeploymentEngineBase.DeploymentModes.RedeployNewVersion;
                }
                else
                {
                    if (getValue<bool>(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeployVersion))
                        Deployment.DeploymentMode = DeploymentEngineBase.DeploymentModes.DeployPriorVersion;
                    else
                        Deployment.DeploymentMode = DeploymentEngineBase.DeploymentModes.DeployNewApplication;
                }


                Deployment.CreateNewEnvironment =
                    getValue<bool>(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv);
                Deployment.EnvironmentName =
                    getValue<string>(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName);
                Deployment.EnvironmentDescription = getValue<string>(BeanstalkDeploymentWizardProperties
                    .EnvironmentProperties.propkey_EnvDescription);
                Deployment.EnvironmentCNAME =
                    getValue<string>(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CName);
                Deployment.EnvironmentType =
                    getValue<string>(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType);
                Deployment.LoadBalancerType = getValue<string>(BeanstalkDeploymentWizardProperties.EnvironmentProperties
                    .propkey_LoadBalancerType);

                Deployment.KeyPairName = getValue<string>(DeploymentWizardProperties.AWSOptions.propkey_KeyPairName);
                if (getValue<bool>(DeploymentWizardProperties.AWSOptions.propkey_CreateKeyPair))
                {
                    CreateKeyPair(Account, region, Deployment.KeyPairName);
                }


                Deployment.SolutionStack =
                    getValue<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack);
                Deployment.CustomAmiID = getValue<string>(DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID);

                Deployment.RoleName = ConfigureIAMRole(Account, region);
                Deployment.ServiceRoleName =
                    getValue<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ServiceRoleName);

                // if user did not choose to use a custom vpc and we are in a vpc-only environment, push through the default vpc id so we
                // create resources in the right place from the get-go, and don't rely on the service to 'notice'
                GetVpcDetails(out var launchIntoVpc, out var vpcId);
                Deployment.LaunchIntoVPC = launchIntoVpc;
                Deployment.VPCId = vpcId;

                string enableXRayDaemon = getValue<string>(BeanstalkDeploymentWizardProperties.ApplicationProperties
                    .propkey_EnableXRayDaemon);
                if (!string.IsNullOrEmpty(enableXRayDaemon))
                {
                    Deployment.EnableXRayDaemon = Convert.ToBoolean(enableXRayDaemon);

                    if (Deployment.EnableXRayDaemon.GetValueOrDefault())
                    {
                        deployMetric.XrayEnabled = true;
                    }
                }

                string enableEnhancedHealth = getValue<string>(BeanstalkDeploymentWizardProperties.ApplicationProperties
                    .propkey_EnableEnhancedHealth);
                if (!string.IsNullOrEmpty(enableEnhancedHealth))
                {
                    Deployment.EnableEnhancedHealth = Convert.ToBoolean(enableEnhancedHealth);

                    if (Deployment.EnableEnhancedHealth.GetValueOrDefault())
                    {
                        deployMetric.EnhancedHealthEnabled = true;
                    }
                }

                Deployment.VPCSecurityGroupId = getValue<string>(BeanstalkDeploymentWizardProperties
                    .AWSOptionsProperties.propkey_VPCSecurityGroup);
                Deployment.InstanceSubnetId =
                    getValue<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceSubnet);
                Deployment.ELBSubnetId =
                    getValue<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ELBSubnet);
                Deployment.ELBScheme =
                    getValue<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ELBScheme);

                if (string.IsNullOrEmpty(Deployment.ELBSubnetId) && string.Equals(Deployment.EnvironmentType,
                    BeanstalkConstants.EnvType_LoadBalanced, StringComparison.OrdinalIgnoreCase))
                {
                    Deployment.ELBSubnetId = GetDefaultVPCSubnet(Account, region);
                }

                Deployment.EnableConfigRollingDeployment = getValue<bool>(BeanstalkDeploymentWizardProperties
                    .RollingDeployments.propKey_EnableConfigRollingDeployment);
                if (Deployment.EnableConfigRollingDeployment)
                {
                    Deployment.ConfigRollingDeploymentMaximumBatchSize = getValue<int>(
                        BeanstalkDeploymentWizardProperties.RollingDeployments.propKey_ConfigMaximumBatchSize);
                    Deployment.ConfigRollingDeploymentMinimumInstancesInServices = getValue<int>(
                        BeanstalkDeploymentWizardProperties.RollingDeployments.propKey_ConfigMinInstanceInServices);
                }

                if (DeploymentProperties.ContainsKey(BeanstalkDeploymentWizardProperties.RollingDeployments
                    .propKey_AppBatchType))
                {
                    Deployment.AppRollingDeploymentBatchType = getValue<string>(BeanstalkDeploymentWizardProperties
                        .RollingDeployments.propKey_AppBatchType);
                    Deployment.AppRollingDeploymentBatchSize = getValue<int>(BeanstalkDeploymentWizardProperties
                        .RollingDeployments.propKey_AppBatchSize);
                }


                Deployment.UseIncrementalDeployment
                    = getValue<bool>(BeanstalkDeploymentWizardProperties.DeploymentModeProperties
                        .propkey_IncrementalDeployment);
                if (Deployment.UseIncrementalDeployment)
                {
                    Deployment.IncrementalPushRepositoryLocation
                        = getValue<string>(BeanstalkDeploymentWizardProperties.DeploymentModeProperties
                            .propkey_IncrementalPushRepositoryLocation);
                }
                else
                {
                    Deployment.UploadBucket = DetermineBucketName(Account, region);
                }

                Deployment.InstanceTypeID =
                    getValue<string>(DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeID);

                CopyApplicationOptionProperties();

                Deployment.RDSSecurityGroups =
                    getValue<List<string>>(
                        BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_RDSSecurityGroups);
                Deployment.VPCSecurityGroups =
                    getValue<List<string>>(
                        BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_VPCSecurityGroups);
                Deployment.VPCGroupsAndReferencingDBInstances =
                    getValue<Dictionary<string, List<int>>>(BeanstalkDeploymentWizardProperties.DatabaseOptions
                        .propkey_VPCGroupsAndDBInstances);

                if (Deployment.DeploymentMode == DeploymentEngineBase.DeploymentModes.DeployNewApplication ||
                    Deployment.DeploymentMode == DeploymentEngineBase.DeploymentModes.DeployPriorVersion)
                    Deployment.Deploy();
                else
                    Deployment.Redeploy();

                success = true;
            }
            catch (Exception e)
            {
                deployMetric.AddErrorMetadata(e);

                string errMsg = string.Format("Error publishing application: {0}", e.Message);
                Observer.Error(errMsg);
                _toolkitContext.ToolkitHost.ShowError("Publish Error", errMsg);
            }
            finally
            {
                try
                {
                    duration.Stop();
                    deployMetric.Result = success ? Result.Succeeded : Result.Failed;
                    deployMetric.Duration = duration.Elapsed.TotalMilliseconds;
                    deployMetric.AwsAccount = GetAccountId();
                    _toolkitContext.TelemetryLogger.RecordBeanstalkDeploy(deployMetric);

                    _toolkitContext.ToolkitHost.UpdateStatus(string.Empty);
                    if (success)
                    {
                        Observer.Status("Publish to AWS Elastic Beanstalk environment '{0}' completed successfully",
                            Deployment.EnvironmentName);

                        DeploymentTaskNotifier notifier = new DeploymentTaskNotifier();
                        notifier.BeanstalkClient = Deployment.BeanstalkClient;
                        notifier.ApplicationName = Deployment.ApplicationName;
                        notifier.EnvironmentName = Deployment.EnvironmentName;
                        TaskWatcher.WatchAndNotify(TaskWatcher.DefaultPollInterval, notifier);

                        SelectNewTreeItems(Deployment.ApplicationName, Deployment.EnvironmentName);
                    }
                    else
                        Observer.Status(
                            "Publish to AWS Elastic Beanstalk environment '{0}' did not complete successfully",
                            Deployment.EnvironmentName);
                }
                catch (Exception e)
                {
                    LOGGER.Error("Error selecting new tree items", e);
                }
            }
        }

        private string GetAccountId()
        {
            return _toolkitContext.ConnectionManager.ActiveAccountId ?? MetadataValue.Invalid;
        }

        void CopyApplicationOptionProperties()
        {
            if (Deployment.DeploymentMode != DeploymentEngineBase.DeploymentModes.DeployNewApplication)
            {
                // only update config if really changed, otherwise we are subject to two UpdateEnvironment
                // calls due to current beanstalk api restriction
                if (!getValue<bool>(BeanstalkDeploymentWizardProperties.AppOptionsProperties.propkey_AppOptionsUpdated))
                    return;
            }

            if (DeploymentProperties.ContainsKey(DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl))
                Deployment.ApplicationHealthcheckPath =
                    getValue<string>(DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl);
            else if (Deployment.DeploymentMode == DeploymentEngineBase.DeploymentModes.DeployNewApplication)
                Deployment.ApplicationHealthcheckPath = "/";

            string enable32Bit =
                getValue<string>(DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications);
            if (!string.IsNullOrEmpty(enable32Bit))
                Deployment.Enable32BitApplications = Convert.ToBoolean(enable32Bit);
            Deployment.TargetRuntime = getValue<string>(DeploymentWizardProperties.AppOptions.propkey_TargetRuntime);

            if (DeploymentProperties.ContainsKey(DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings))
            {
                var appSettings =
                    DeploymentProperties[DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings] as
                        IDictionary<string, string>;

                foreach (var kvp in appSettings)
                {
                    if (string.Equals(kvp.Key, DeploymentWizardProperties.AppOptions.propkey_EnvAccessKey))
                    {
                        Deployment.SetConfigurationOption("aws:elasticbeanstalk:application:environment",
                            "AWS_ACCESS_KEY_ID",
                            kvp.Value);
                    }
                    else if (string.Equals(kvp.Key, DeploymentWizardProperties.AppOptions.propkey_EnvSecretKey))
                    {
                        Deployment.SetConfigurationOption("aws:elasticbeanstalk:application:environment",
                            "AWS_SECRET_KEY",
                            kvp.Value);
                    }
                    else
                    {
                        Deployment.SetConfigurationOption("aws:elasticbeanstalk:application:environment",
                            kvp.Key,
                            kvp.Value);
                    }
                }
            }

            if (DeploymentProperties.ContainsKey(BeanstalkDeploymentWizardProperties.AppOptionsProperties
                .propkey_NotificationEmail))
                Deployment.SetConfigurationOption("aws:elasticbeanstalk:sns:topics",
                    "Notification Endpoint",
                    getValue<string>(BeanstalkDeploymentWizardProperties.AppOptionsProperties
                        .propkey_NotificationEmail));
        }


        static string DetermineBucketName(AccountViewModel account, ToolkitRegion region)
        {
            string bucketName = null;

            // ticket 0022500483, prefer Beanstalk's CreateStorageLocation but if that fails, fall back to using 
            // information we have at hand
            try
            {
                var client = account.CreateServiceClient<AmazonElasticBeanstalkClient>(region);
                var response = client.CreateStorageLocation();
                bucketName = response.S3Bucket;
            }
            catch (AmazonElasticBeanstalkException e)
            {
                LOGGER.ErrorFormat(
                    "Exception {0} from CreateStorageLocation, falling back to manual construction of bucket name.",
                    e.Message);
            }
            finally
            {
                if (string.IsNullOrEmpty(bucketName))
                {
                    var accountId = account.ToolkitContext.ServiceClientManager.GetAccountId(new AwsConnectionSettings(account.Identifier, region));
                    if (string.IsNullOrWhiteSpace(accountId))
                    {
                        throw new Exception("Unable to determine account for use with S3 Buckets");
                    }
                    bucketName = string.Format("elasticbeanstalk-{0}-{1}", region.Id, accountId)
                        .ToLower();
                }
            }

            LOGGER.DebugFormat("Deployment uploads assigned to bucket {0}", bucketName);
            return bucketName;
        }
    }

    /// <summary>
    /// Internal class wraps checking for deployment completion and eventual notification if
    /// it succeeds
    /// </summary>
    internal class DeploymentTaskNotifier : TaskWatcher.IQueryTaskCompletionProxy,
        TaskWatcher.INotifyTaskCompletionProxy
    {
        public IAmazonElasticBeanstalk BeanstalkClient { get; set; }
        public string ApplicationName { get; set; }
        public string EnvironmentName { get; set; }
        string _endpointUrl = string.Empty;

        #region IQueryTaskCompletionProxy Members

        public TaskWatcher.TaskCompletionState QueryTaskCompletion(TaskWatcher callingNotifier)
        {
            TaskWatcher.TaskCompletionState completionState = TaskWatcher.TaskCompletionState.pending;

            try
            {
                var response = BeanstalkClient.DescribeEnvironments
                (new DescribeEnvironmentsRequest()
                {
                    EnvironmentNames = new List<string>() {this.EnvironmentName},
                    ApplicationName = this.ApplicationName
                });

                if (string.Compare(response.Environments[0].Health, "green", true) == 0)
                {
                    completionState = TaskWatcher.TaskCompletionState.completed;
                    this._endpointUrl = string.Format("http://{0}/", response.Environments[0].CNAME);
                }
                else if (string.Compare(response.Environments[0].Health, "red", true) == 0)
                    completionState = TaskWatcher.TaskCompletionState.error;
            }
            catch (Exception)
            {
            }

            return completionState;
        }

        #endregion

        #region INotifyTaskCompletionProxy Members

        public void NotifyTaskCompletion(TaskWatcher callingNotifier)
        {
            bool success = callingNotifier.WatchingState == TaskWatcher.WatcherState.completedOK;
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action) (() =>
            {
                AWSNotificationToaster toaster = new AWSNotificationToaster();
                DeploymentNotificationPanel panel = new DeploymentNotificationPanel();
                panel.SetPanelContent(ApplicationName, EnvironmentName, this._endpointUrl, success);
                toaster.ShowNotification(panel, "AWS Elastic Beanstalk");
            }));
        }

        #endregion
    }
}
