﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Util;
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
        
        protected static ILog Logger;
        protected IDictionary<string, object> DeploymentProperties { get; }
        protected ToolkitContext ToolkitContext { get; set; }

        const int MAX_REFRESH_RETRIES = 3;
        const int SLEEP_TIME_BETWEEN_REFRESHES = 500;

        public DeploymentControllerBase(string deploymentPackage, IDictionary<string, object> deploymentProperties, ToolkitContext toolkitContext)
        {
            _account = CommonWizardProperties.AccountSelection.GetSelectedAccount(deploymentProperties);
            if (_account == null)
                throw new System.InvalidOperationException("Missing account data in deployment properties; cannot proceed");
            ToolkitContext = toolkitContext;
            DeploymentProperties = deploymentProperties;

            Deployment = DeploymentEngineFactory.CreateEngine(DeploymentEngineFactory.CloudFormationServiceName, toolkitContext) 
                    as CloudFormationDeploymentEngine;
            Deployment.DeploymentPackage = deploymentPackage;
            Deployment.Region = Region.Id;

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
                    this._ec2Client = _account.CreateServiceClient<AmazonEC2Client>(Region);
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
                    this._cfClient = _account.CreateServiceClient<AmazonCloudFormationClient>(Region);
                }

                return _cfClient;
            }
        }

        ToolkitRegion _region = null;
        [Browsable(false)]
        protected ToolkitRegion Region
        {
            get
            {
                if (_region == null)
                {
                    _region = CommonWizardProperties.AccountSelection.GetSelectedRegion(DeploymentProperties);
                }

                return _region;
            }
        }

        /// <summary>
        /// Can be used to generate a default and predictable name for the upload bucket 
        /// if one is not supplied
        /// </summary>
        public static string DefaultBucketName(AccountViewModel account, ToolkitRegion region)
        {
            return Util.CloudFormationUtil.CreateCloudFormationUploadBucketName(
                account,
                region,
                "awsdeployment");
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
            ToolkitContext.ToolkitHost.OutputToHostConsole(message, true);
            ToolkitContext.ToolkitHost.UpdateStatus(message);
            Logger.InfoFormat("Publish to AWS CloudFormation: {0}", message);
        }

        protected void CreateKeyPair(AccountViewModel account)
        {
            string keyName = DeploymentProperties[DeploymentWizardProperties.AWSOptions.propkey_KeyPairName] as string;

            var request = new CreateKeyPairRequest() { KeyName = keyName };
            var response = EC2Client.CreateKeyPair(request);

            Logger.Debug("key pair created");
            KeyPairLocalStoreManager.Instance.SavePrivateKey(account.SettingsUniqueKey, Region.Id, keyName,
                response.KeyPair.KeyMaterial);
            Logger.Debug("key pair created, stored in local store");
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
                ToolkitContext.ToolkitHost.BeginExecuteOnUIThread((Action)(() => stack.ExecuteDefaultAction() /* meta.OnOpen(stack)*/));

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
                Ipv4Ranges = new List<IpRange>() { new IpRange {CidrIp = cidrIP } },
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
