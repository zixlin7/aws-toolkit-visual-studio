using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageUI;
using Amazon.AWSToolkit.Util;

using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.EC2;
using Amazon.EC2.Model;

using AWSDeployment;
using log4net;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    abstract class DeploymentControllerBase 
    {
        protected AccountViewModel _account;

        protected CloudFormationDeploymentEngine Deployment { get; set; }
        protected DeploymentControllerBaseObserver Observer { get; set; }
        
        protected static ILog LOGGER;
        protected IDictionary<string, object> DeploymentProperties { get; private set; }

        const int MAX_REFRESH_RETRIES = 3;
        const int SLEEP_TIME_BETWEEN_REFRESHES = 500;

        public DeploymentControllerBase(string deploymentPackage, IDictionary<string, object> deploymentProperties)
        {
            _account = deploymentProperties[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
            if (_account == null)
                throw new InvalidOperationException("Missing account data in deployment properties; cannot proceed");

            DeploymentProperties = deploymentProperties;

            Deployment = DeploymentEngineFactory.CreateEngine(DeploymentEngineFactory.CloudFormationServiceName) 
                    as CloudFormationDeploymentEngine;
            Deployment.DeploymentPackage = deploymentPackage;
            Deployment.Region = RegionEndPoints.SystemName;

            // inject default ami (2008/2012) if selected; if the user chose a custom ami, it will be applied later
            // as part of shared custom ami logic with Beanstalk
            if (deploymentProperties.ContainsKey(CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_ContainerAMI))
            {
                var amiID = deploymentProperties[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_ContainerAMI] as string;
                Deployment.TemplateParameters.Add("AmazonMachineImage", amiID);
            }
        }

        public void Execute(object state)
        {
            Execute();
        }

        public abstract void Execute();

        IAmazonEC2 _ec2Client = null;
        [Browsable(false)]
        protected IAmazonEC2 EC2Client
        {
            get
            {
                if (this._ec2Client == null)
                {
                    var ec2Config = new AmazonEC2Config {ServiceURL = RegionEndPoints.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME).Url};
                    this._ec2Client = new AmazonEC2Client(_account.Credentials, ec2Config);
                }

                return this._ec2Client;
            }
        }

        IAmazonCloudFormation _cfClient = null;
        protected IAmazonCloudFormation CloudFormationClient
        {
            get
            {
                if (this._cfClient == null)
                {
                    var cfConfig = new AmazonCloudFormationConfig {ServiceURL = RegionEndPoints.GetEndpoint(RegionEndPointsManager.CLOUDFORMATION_SERVICE_NAME).Url};
                    this._cfClient = new AmazonCloudFormationClient(_account.Credentials, cfConfig);
                }

                return _cfClient;
            }
        }

        RegionEndPointsManager.RegionEndPoints _regionEndPoints = null;
        [Browsable(false)]
        protected RegionEndPointsManager.RegionEndPoints RegionEndPoints
        {
            get
            {
                if (_regionEndPoints == null)
                {
                    _regionEndPoints = DeploymentProperties[CommonWizardProperties.AccountSelection.propkey_SelectedRegion]
                                            as RegionEndPointsManager.RegionEndPoints;
                }

                return _regionEndPoints;
            }
        }

        /// <summary>
        /// Can be used to generate a default and predictable name for the upload bucket 
        /// if one is not supplied
        /// </summary>
        /// <param name="account"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        public static string DefaultBucketName(AccountViewModel account, string region)
        {
            // had occasion when users have had lead/trail spaces when entering a/c number; explorer
            // should remove but play safe. Know access key is trim-safe already.
            string suffix = account.UniqueIdentifier;
            var bucketName = string.Format("awsdeployment-{0}-{1}", region, suffix);
            return bucketName.ToLower(); 
        }

        /// <summary>
        /// Can be used to generate a default and predictable key for the config file if not not supplied
        /// </summary>
        /// <param name="stackName"></param>
        /// <returns></returns>
        public static string DefaultConfigFileKey(string stackName)
        {
            return string.Format("{0}/{0}.config", stackName);
        }

        protected void WriteOutputMessage(string message)
        {
            ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(message, true);
            ToolkitFactory.Instance.ShellProvider.UpdateStatus(message);
            LOGGER.InfoFormat("Publish to AWS CloudFormation: {0}", message);
        }

        protected void CreateKeyPair(AccountViewModel account)
        {
            string keyName = DeploymentProperties[DeploymentWizardProperties.AWSOptions.propkey_KeyPairName] as string;

            var request = new CreateKeyPairRequest() { KeyName = keyName };
            var response = EC2Client.CreateKeyPair(request);

            LOGGER.Debug("key pair created");
            IEC2RootViewModel ec2Root = account.FindSingleChild<IEC2RootViewModel>(false);
            KeyPairLocalStoreManager.Instance.SavePrivateKey(account, RegionEndPoints.SystemName, keyName,
                response.KeyPair.KeyMaterial);
            LOGGER.Debug("key pair created, stored in local store");
        }

        protected void SelectNewTreeItems(AccountViewModel account)
        {
            if (account == null)
                return;

            string stackName = DeploymentProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;

            bool showStatus = false;
            if (DeploymentProperties.ContainsKey(DeploymentWizardProperties.ReviewProperties.propkey_LaunchStatusOnClose))
                showStatus = (bool)DeploymentProperties[DeploymentWizardProperties.ReviewProperties.propkey_LaunchStatusOnClose];

            var serviceRoot = account.FindSingleChild<CloudFormationRootViewModel>(false);
            CloudFormationStackViewModel stack = null;
            for (int i = 0; stack == null  && i < MAX_REFRESH_RETRIES; i++)
            {
                if (stack == null)
                {
                    serviceRoot.Refresh(false);
                    stack = serviceRoot.FindSingleChild<CloudFormationStackViewModel>(false, x => x.StackName == stackName);

                    if (stack != null)
                        break;
 
                    // Didn't find the new stack, sleeping a little to let the service catch up.
                    Thread.Sleep(SLEEP_TIME_BETWEEN_REFRESHES);
                }
            }

            if (stack != null && showStatus)
            {
                CloudFormationStackViewMetaNode meta = stack.MetaNode as CloudFormationStackViewMetaNode;
                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() => stack.ExecuteDefaultAction() /* meta.OnOpen(stack)*/));

                ToolkitFactory.Instance.Navigator.SelectedNode = stack;
            }
        }

        protected Parameter FindParameter(string paramKey, IEnumerable<Parameter> parameters)
        {
            if (parameters != null)
            {
                foreach (Parameter param in parameters)
                {
                    if (param.ParameterKey == paramKey)
                        return param;
                }
            }

            return null;
        }

        protected void CopyContainerProperties()
        {
            // don't overwrite engine defaults unless set
            if (DeploymentProperties.ContainsKey(DeploymentWizardProperties.AppOptions.propkey_TargetRuntime))
                Deployment.TargetRuntime = getValue<string>(DeploymentWizardProperties.AppOptions.propkey_TargetRuntime);
            if (DeploymentProperties.ContainsKey(DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications))
                Deployment.Enable32BitApplications = getValue<bool>(DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications);
            if (DeploymentProperties.ContainsKey(DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl))
                Deployment.ApplicationHealthcheckPath = getValue<string>(DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl);
        }

        protected void CopyApplicationOptionProperties()
        {
            if (DeploymentProperties.ContainsKey(DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings))
            {
                var appSettings = DeploymentProperties[DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings] as IDictionary<string, string>;

                foreach (var pk in CommonAppOptionsPageControllerBase.AppParamKeys)
                {
                    if (appSettings.ContainsKey(pk) && appSettings[pk] != null)
                        Deployment.EnvironmentProperties[pk] = appSettings[pk];
                }

                if (appSettings.ContainsKey(DeploymentWizardProperties.AppOptions.propkey_EnvAccessKey))
                {
                    var v = appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvAccessKey];
                    if (v != null)
                        Deployment.EnvironmentProperties["AWSAccessKey"] = v;
                }

                if (appSettings.ContainsKey(DeploymentWizardProperties.AppOptions.propkey_EnvSecretKey))
                {
                    var v = appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvSecretKey];
                    if (v != null)
                        Deployment.EnvironmentProperties["AWSSecretKey"] = v;
                }
            }
        }

        protected void OpenIngressForGroup(string groupName, string cidrIP, string protocol, int port)
        {
            var request = new AuthorizeSecurityGroupIngressRequest() { GroupName = groupName };

            var ip = new IpPermission()
            {
                IpRanges = new List<string>() { cidrIP },
                IpProtocol = protocol.ToLower(),
                FromPort = port,
                ToPort = port
            };
            request.IpPermissions.Add(ip);
            EC2Client.AuthorizeSecurityGroupIngress(request);
        }

        protected T getValue<T>(string key)
        {
            object value;
            if (DeploymentProperties.TryGetValue(key, out value))
            {
                T convertedValue = (T)Convert.ChangeType(value, typeof(T));
                return convertedValue;
            }

            return default(T);
        }
    }
}
